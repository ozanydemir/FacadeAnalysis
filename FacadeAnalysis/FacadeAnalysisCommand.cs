using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Display;
using System.Drawing;
using static Rhino.ApplicationSettings.CurvatureAnalysisSettings;
using Rhino.Render.ChangeQueue;
using Mesh = Rhino.Geometry.Mesh;

namespace FacadeAnalysis
{
    public class FacadeAnalysisCommand : Command
    {
        public override string EnglishName => "FacadeAnalysisCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Initialize area and count variables for green and red classification
            // Yeşil ve kırmızı sınıflandırması için alan ve sayı değişkenlerini başlat
            double totalGreenArea = 0;
            int greenCount = 0;
            double totalRedArea = 0;
            int redCount = 0;
            int totalSelectedCount = 0;

            List<Guid> greenObjects = new List<Guid>();
            List<Guid> redObjects = new List<Guid>();

            // Set up object selection parameters
            // Nesne seçimi parametrelerini ayarla
            var objectSelection = new GetObject();
            objectSelection.SetCommandPrompt("Select a panel for curvature analysis.");
            objectSelection.GeometryFilter = ObjectType.Brep;
            objectSelection.SubObjectSelect = false;
            objectSelection.GetMultiple(1, 0);

            if (objectSelection.CommandResult() != Result.Success)
                return objectSelection.CommandResult();

            totalSelectedCount = objectSelection.ObjectCount;

            foreach (var objRef in objectSelection.Objects())
            {
                var brep = objRef.Brep();
                if (brep == null) continue;

                BrepFace mainFace = null;
                double maxArea = 0.0;

                // Retrieve the corner points of the Brep
                // Brep'in köşe noktalarını al
                Point3d[] corners = brep.DuplicateVertices();
                double maxDistance = 0.0;

                // Find the maximum distance between corner points
                // Köşe noktaları arasındaki en büyük mesafeyi bul
                for (int i = 0; i < corners.Length; i++)
                {
                    for (int j = i + 1; j < corners.Length; j++)
                    {
                        double distance = corners[i].DistanceTo(corners[j]);
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                        }
                    }
                }

                foreach (var face in brep.Faces)
                {
                    var faceBrep = face.DuplicateFace(false);
                    var area = AreaMassProperties.Compute(faceBrep).Area;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        mainFace = face;
                    }
                }

                if (mainFace != null)
                {
                    double curvatureValue = CalculateCurvatureValue(mainFace.DuplicateFace(false));

                    // Set the color criteria
                    // Renk belirleme kriterleri
                    bool isCurvatureOk = curvatureValue > 3500;
                    bool isDiameterOk = maxDistance <= 5500; // in mm

                    Log($"Diameter Measure: {maxDistance}");
                    var color = (isCurvatureOk & isDiameterOk) ? System.Drawing.Color.Green : System.Drawing.Color.Red;

                    var obj = objRef.Object();
                    obj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                    obj.Attributes.ObjectColor = color;
                    obj.CommitChanges();

                    if (color == System.Drawing.Color.Green)
                    {
                        totalGreenArea += maxArea;
                        greenCount++;
                        greenObjects.Add(objRef.ObjectId);
                    }
                    else
                    {
                        totalRedArea += maxArea;
                        redCount++;
                        redObjects.Add(objRef.ObjectId);
                    }
                }
            }

            // Group the green and red objects
            // Yeşil ve Kırmızı nesneleri grupla
            GroupObjects(doc, greenObjects, "GreenGroup");
            GroupObjects(doc, redObjects, "RedGroup");

            // Create and display notes
            // Notları oluştur ve göster
            string notes = $"Total Selected Panels: {totalSelectedCount}\n" +
                           $"Total area of green panels: {totalGreenArea:N2} m²\nTotal Count: {greenCount}\n" +
                           $"Total area of red panels: {totalRedArea:N2} m²\nTotal Count: {redCount}";
            doc.Notes = notes;

            doc.Views.Redraw();
            return Result.Success;
        }

        // Grouping Function
        // Gruplama Fonksiyonu
        private int FindOrCreateNewGroup(RhinoDoc doc, string groupName)
        {
            int groupIndex = doc.Groups.Find(groupName);
            if (groupIndex == -2147483647)
            {
                groupIndex = doc.Groups.Add(groupName);
            }

            return groupIndex;
        }

        private void GroupObjects(RhinoDoc doc, List<Guid> objectIds, string groupName)
        {
            int groupIndex = FindOrCreateNewGroup(doc, groupName);

            // Retrieve the IDs of existing group members
            // Mevcut grup üyelerinin ID'lerini al
            var groupMemberIds = new HashSet<Guid>();
            var groupMembers = doc.Groups.GroupMembers(groupIndex);
            if (groupMembers != null)
            {
                foreach (var member in groupMembers)
                {
                    groupMemberIds.Add(member.Id);
                }
            }

            // Check each object ID for group membership and add to group if necessary
            // Her bir nesne ID'si için grup üyeliğini kontrol et ve gerekirse gruba ekle
            foreach (var id in objectIds)
            {
                if (!groupMemberIds.Contains(id))
                {
                    doc.Groups.AddToGroup(groupIndex, id);
                }
            }

            

        }
        private double CalculateCurvatureValue(Brep brep)
        {
            // Start logging
            // Log ekleme
            Log("CalculateCurvatureValue başlatılıyor.");

            // Get the default CurvatureAnalysisSettingsState object
            // Varsayılan CurvatureAnalysisSettingsState nesnesini al
            CurvatureAnalysisSettingsState settings = CurvatureAnalysisSettings.GetCurrentState();
            settings.Style = CurvatureStyle.MinRadius; // Set the Style to MinRadius
                                                       // Style'ı MinRadius olarak ayarla

            var meshingParameters = MeshingParameters.DefaultAnalysisMesh;

            // Log meshing parameters
            // Meshing parametrelerini logla
            Log($"MaxEdgeLen: {meshingParameters.MaximumEdgeLength}, GridAngle: {meshingParameters.GridAngle}, GridAspectRatio: {meshingParameters.GridAspectRatio}, GridMinCount: {meshingParameters.GridMinCount}, RefineMesh: {meshingParameters.RefineGrid}");

            var meshes = Mesh.CreateFromBrep(brep, meshingParameters);
            if (meshes == null || meshes.Length == 0)
            {
                Log("Mesh could not be created.");
                // If mesh cannot be created, return 0
                // Eğer mesh oluşturulamazsa 0 dön
                return 0;
            }

            // Check the validity of the meshes
            // Meshlerin geçerliliğini kontrol et
            foreach (var mesh in meshes)
            {
                if (!mesh.IsValid)
                {
                    Log("Invalid mesh detected.");
                    // If an invalid mesh is found, return 0
                    // Geçersiz mesh tespit edilirse, 0 dön
                    return 0;
                }
            }

            try
            {
                bool autoRangeResult = CurvatureAnalysisSettings.CalculateCurvatureAutoRange(meshes, ref settings);
                if (autoRangeResult)
                {
                    // Successful calculation
                    // Başarılı hesaplama
                    var minRadiusRange = settings.MinRadiusRange;
                    Log($"MinRadiusRange minimum value: {minRadiusRange.T0}");
                    // Return the minimum value of MinRadiusRange
                    // MinRadiusRange'nin minimum değerini dön
                    return minRadiusRange.T0;
                }
                else
                {
                    // Unsuccessful calculation
                    // Başarısız hesaplama
                    Log("Auto range calculation failed.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                return 0;
            }
        }

        private void Log(string message)
        {
            RhinoApp.WriteLine(message);
        }
    }
}
