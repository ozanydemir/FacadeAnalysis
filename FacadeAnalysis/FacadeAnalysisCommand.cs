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

        //protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        //{
        //    // Kullanıcıdan yüzey seçimi alın
        //    var objectSelection = new GetObject();
        //    objectSelection.SetCommandPrompt("Eğrilik analizi için panelleri seçin.");
        //    objectSelection.GeometryFilter = ObjectType.Surface;
        //    objectSelection.GroupSelect = false;
        //    objectSelection.SubObjectSelect = true;
        //    //objectSelection.GroupSelect = true;
        //    //objectSelection.SubObjectSelect = false;
        //    objectSelection.GetMultiple(1, 0);

        //    if (objectSelection.CommandResult() != Result.Success)
        //        return objectSelection.CommandResult();

        //    foreach (var objRef in objectSelection.Objects())
        //    {
        //        var brep = objRef.Brep();
        //        if (brep == null)
        //            continue;

        //        //Her bir brep için curcature değerini hesapla
        //        double curvatureValue = CalculateCurvatureValue(brep);

        //        // Curvature değerine göre renk belirle
        //        var color = (curvatureValue > 3500) ? System.Drawing.Color.Green : System.Drawing.Color.Red;

        //        // Brep'in rengini değiştir
        //        var obj = doc.Objects.Find(objRef.ObjectId);
        //        if (obj != null)
        //        {
        //            obj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
        //            obj.Attributes.ObjectColor = color;
        //            obj.CommitChanges();
        //        }
        //    }
        //    doc.Views.Redraw();
        //    return Result.Success;
        //}

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            double totalGreenArea = 0;
            int greenCount = 0;
            double totalRedArea = 0;
            int redCount = 0;
            int totalSelectedCount = 0;


            var objectSelection = new GetObject();
            objectSelection.SetCommandPrompt("Eğrilik analizi için bir panel seçin.");
            objectSelection.GeometryFilter = ObjectType.Brep;
            objectSelection.SubObjectSelect = false;
            objectSelection.GetMultiple(1, 0);

            if (objectSelection.CommandResult() != Result.Success)
                return objectSelection.CommandResult();

            foreach (var objRef in objectSelection.Objects())
            {
                var brep = objRef.Brep();
                if (brep == null) continue;

                string panelName = objRef.Object().Name;

                BrepFace mainFace = null;
                double maxArea = 0.0;
                int totalPanelCount = objectSelection.Objects().Length;

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
                    // Ana yüzey üzerinde eğrilik analizi yap
                    double curvatureValue = CalculateCurvatureValue(mainFace.DuplicateFace(false));

                    // Eğrilik değerine göre renk belirle
                    var color = (curvatureValue > 3500) ? System.Drawing.Color.Green : System.Drawing.Color.Red;
                    string layerName = (color == System.Drawing.Color.Green) ? "GREEN" : "RED";

                    //Yeşile boyanan nesnelerin alanı ve toplam sayısını hesapla
                    if(color == System.Drawing.Color.Green)
                    {
                        var faceArea = AreaMassProperties.Compute(mainFace).Area;
                        totalGreenArea += faceArea;
                        string formattedGreenArea = totalGreenArea.ToString("N2");
                        greenCount++;
                    }

                    //Yeşile boyanan nesnelerin alanı ve toplam sayısını hesapla
                    if (color == System.Drawing.Color.Red)
                    {
                        var faceArea = AreaMassProperties.Compute(mainFace).Area;
                        totalRedArea += faceArea;
                        string formattedRedArea = totalRedArea.ToString("N2");
                        redCount++;
                    }

                    //Katmanı kontrol et ve yoksa oluştur
                    Layer layer = doc.Layers.FindName(layerName);
                    int layerIndex;

                    if(layer == null)
                    {
                        layer = new Layer
                        {
                            Name = layerName,
                            Color = color
                        };
                        layerIndex = doc.Layers.Add(layer);
                    }
                    else
                    {
                        layerIndex = layer.Index;
                    }
                    // Seçilen nesnenin renk özelliklerini güncelle
                    var obj = objRef.Object();
                    obj.Attributes.LayerIndex = layerIndex;
                    obj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                    obj.Attributes.ObjectColor = color;
                    obj.CommitChanges();

                }

                
            }


            //Total Veri Notu
            
            string notes = doc.Notes;
            string totalCount = $"Seçilen Toplam Panel Sayısı: {totalSelectedCount}";
            string noteGreen = $"Yeşil panellerin toplam alanı: {totalGreenArea} m²\nToplam Adedi: {greenCount}";
            string noteRed = $"Kırmızı panellerin toplam alanı: {totalRedArea} m²\nToplam Adedi: {redCount}";
            notes += totalCount + "\n" + noteGreen + "\n" + noteRed;

            doc.Notes = notes;
            RhinoApp.RunScript("Notes", false);

            doc.Views.Redraw();
            return Result.Success;
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
