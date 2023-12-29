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
            // Değişken tanımlamaları
            double totalGreenArea = 0;
            int greenCount = 0;
            double totalRedArea = 0;
            int redCount = 0;
            int totalSelectedCount = 0;

            List<Guid> greenObjects = new List<Guid>();
            List<Guid> redObjects = new List<Guid>();

            var objectSelection = new GetObject();
            objectSelection.SetCommandPrompt("Eğrilik analizi için bir panel seçin.");
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

                // Brep'in köşe noktalarını al
                Point3d[] corners = brep.DuplicateVertices();
                double maxDistance = 0.0;

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

                    //Renk belirleme kriterleri
                    bool isCurvatureOk = curvatureValue > 3500;
                    bool isDiameterOk = maxDistance <= 5500; // mm cinsinden

                    Log($"Çap Ölçüsü: {maxDistance}");
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

            // Yeşil ve Kırmızı nesneleri grupla
            GroupObjects(doc, greenObjects, "GreenGroup");
            GroupObjects(doc, redObjects, "RedGroup");

            // Notları oluştur ve göster
            string notes = $"Seçilen Toplam Panel Sayısı: {totalSelectedCount}\n" +
                           $"Yeşil panellerin toplam alanı: {totalGreenArea:N2} m²\nToplam Adedi: {greenCount}\n" +
                           $"Kırmızı panellerin toplam alanı: {totalRedArea:N2} m²\nToplam Adedi: {redCount}";
            doc.Notes = notes;

            doc.Views.Redraw();
            return Result.Success;
        }

        //Gruplama Fonksiyonu
        private int FindOrCreateNewGroup(RhinoDoc doc, string groupName)
        {
            int grupIndex = doc.Groups.Find(groupName);
            if(grupIndex == -2147483647)
            {
                grupIndex = doc.Groups.Add(groupName);
            }

            return grupIndex;
        }

        private void GroupObjects(RhinoDoc doc, List<Guid> objectIds, string groupName)
        {

            int groupIndex = FindOrCreateNewGroup(doc, groupName);

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
            // Log ekleme
            Log("CalculateCurvatureValue başlatılıyor.");

            // Varsayılan CurvatureAnalysisSettingsState nesnesini al
            CurvatureAnalysisSettingsState settings = CurvatureAnalysisSettings.GetCurrentState();
            settings.Style = CurvatureStyle.MinRadius; // Style'ı MinRadius olarak ayarla

            var meshingParameters = MeshingParameters.DefaultAnalysisMesh;
                       
            //Meshing Parameters Check
            Log($"MaxEdgeLen: {meshingParameters.MaximumEdgeLength}, GridAngle: {meshingParameters.GridAngle}, GridAspectRatio: {meshingParameters.GridAspectRatio}, GridMinCount: {meshingParameters.GridMinCount}, RefineMesh: {meshingParameters.RefineGrid}");

            var meshes = Mesh.CreateFromBrep(brep, meshingParameters);
            if (meshes == null || meshes.Length == 0)
            {
                Log("Mesh oluşturulamadı.");
                return 0; // Eğer mesh oluşturulamazsa 0 dön
            }

            // Meshlerin geçerliliğini kontrol et
            foreach (var mesh in meshes)
            {
                if (!mesh.IsValid)
                {
                    Log("Geçersiz mesh tespit edildi.");
                    return 0;
                }
            }          

            // Log: Mesh bilgileri
            //foreach (var mesh in meshes)
            //{
            //    Log($"Mesh bilgileri: Vertex sayısı = {mesh.Vertices.Count}, Face sayısı = {mesh.Faces.Count}");
            //}

            // Log: Settings öncesi durumu
            //Log($"Settings ayarları: Style = {settings.Style}, MinRadiusRange = {settings.MinRadiusRange}, MaxRadiusRange = {settings.MaxRadiusRange}");

            try
            {
                bool autoRangeResult = CurvatureAnalysisSettings.CalculateCurvatureAutoRange(meshes, ref settings);
                //Log($"CalculateCurvatureAutoRange sonucu: {autoRangeResult}");

                if (autoRangeResult)
                {
                    // Başarılı hesaplama
                    var minRadiusRange = settings.MinRadiusRange;
                    Log($"MinRadiusRange minimum değeri: {minRadiusRange.T0}");
                    return minRadiusRange.T0;
                }
                else
                {
                    // Başarısız hesaplama
                    Log("Auto range hesaplaması başarısız oldu.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Log($"Hata: {ex.Message}");
                return 0;
            }

        }

        private void Log(string message)
        {
            RhinoApp.WriteLine(message);
        }

    }
}
