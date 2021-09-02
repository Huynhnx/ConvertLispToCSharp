using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace ConvertLispToCSharp
{
    public class Commands
    {
        public static ObjectId AppendEntity(Entity ent)
        {
            ObjectId objId = ObjectId.Null;
            if (ent == null)
            {
                return objId;
            }
            Document doc = Application.DocumentManager.MdiActiveDocument;
            try
            {
                using (doc.LockDocument())
                {
                    Database db = doc.Database;
                    // start new transaction
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        // open model space block table record
                        BlockTableRecord spaceBlkTblRec = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        // append entity to model space block table record
                        objId = spaceBlkTblRec.AppendEntity(ent);
                        trans.AddNewlyCreatedDBObject(ent, true);
                        // finish
                        trans.Commit();
                    }
                }

            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage(ex.ToString());
            }
            return objId;
        }
        public static void DeleteEntity(ObjectId id)
        {
            if (ObjectId.Null == id || id.IsErased)
                return;
            using (Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Database db = Application.DocumentManager.MdiActiveDocument.Database;
                // start new transaction
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    using (DBObject obj = trans.GetObject(id, OpenMode.ForWrite))
                    {
                        if (null != obj && !obj.IsErased)
                        {
                            obj.Erase();
                        }
                    }
                    // finish
                    trans.Commit();
                }
            }
        }
       
        [CommandMethod("DoubleOffset")]
        public static void DoubleOffset()
        {
            //Get document, DataBase and Editor
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;
            //Init Keywords/Option
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\n\nSpecify Offset Distance";
            pKeyOpts.Keywords.Add("Through");
            pKeyOpts.Keywords.Add("Erase");
            pKeyOpts.Keywords.Add("Layer");
            pKeyOpts.Keywords.Default = "Through";
            pKeyOpts.AllowNone = true;
            double distance = 0;
            //Get KeyWord
            PromptResult pKeyRes = ed.GetKeywords(pKeyOpts);
            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.SingleOnly = true;
            opts.MessageForAdding = "Select entities to offset: ";

            PromptSelectionResult selRes = ed.GetSelection(opts);
            if (selRes.Status != PromptStatus.OK)
            {
                return;
            }
            if (selRes.Value.Count != 0 && selRes.Value.Count != 0)
            {
                SelectionSet set = selRes.Value;
                ObjectId[] singleId = set.GetObjectIds();
                if (singleId[0].IsValid == false)
                {
                    return;
                }
                //If Get Keyword seccess
                if (pKeyRes.Status == PromptStatus.OK)
                {
                    if (pKeyRes.StringResult == "Through")
                    {

                        {


                            PromptPointOptions op = new PromptPointOptions("\nSpecify through point or [Exit/Multiple] <Exit> : ", "Exit Multiple");
                            op.AllowArbitraryInput = true;
                            PromptPointResult r = ed.GetPoint(op);
                            if (r.Status == PromptStatus.OK)
                            {
                                using (Transaction tr = db.TransactionManager.StartTransaction())
                                {
                                    Curve ent = tr.GetObject(singleId[0], OpenMode.ForWrite) as Curve;
                                    Point3d pickpoint = r.Value;
                                    distance = ent.GetClosestPointTo(pickpoint, true).DistanceTo(pickpoint);
                                    DBObjectCollection Neoffsetcurs = ent.GetOffsetCurves(distance);
                                    DBObjectCollection Posoffsetcurs = ent.GetOffsetCurves(-distance);
                                    foreach (Entity acEnt in Neoffsetcurs)
                                    {
                                        AppendEntity(acEnt);
                                    }
                                    if (ent is Line)
                                    {
                                        foreach (Entity acEnt in Posoffsetcurs)
                                        {
                                            AppendEntity(acEnt);
                                        }
                                    }
                                    tr.Commit();
                                }

                            }
                            else if (r.Status == PromptStatus.Keyword)
                            {
                                if (r.StringResult == "Exit")
                                {
                                    return;
                                }
                                if (r.StringResult == "Multiple")
                                {
                                    bool stop = true;
                                    while (stop)
                                    {
                                        PromptPointOptions opMul = new PromptPointOptions("\nSpecify through point or [Exit] <next object> : ");
                                        op.AllowArbitraryInput = true;
                                        PromptPointResult res = ed.GetPoint(op);
                                        if (res.Status == PromptStatus.OK)
                                        {
                                            using (Transaction tr = db.TransactionManager.StartTransaction())
                                            {
                                                Curve ent = tr.GetObject(singleId[0], OpenMode.ForWrite) as Curve;
                                                Point3d pickpoint = res.Value;
                                                distance = ent.GetClosestPointTo(pickpoint, true).DistanceTo(pickpoint);
                                                DBObjectCollection Neoffsetcurs = ent.GetOffsetCurves(distance);
                                                DBObjectCollection Posoffsetcurs = ent.GetOffsetCurves(-distance);
                                                foreach (Entity acEnt in Neoffsetcurs)
                                                {
                                                    AppendEntity(acEnt);
                                                }
                                                if (ent is Line)
                                                {
                                                    foreach (Entity acEnt in Posoffsetcurs)
                                                    {
                                                        AppendEntity(acEnt);
                                                    }
                                                }
                                                tr.Commit();
                                            }

                                        }
                                        else
                                        {
                                            stop = false;
                                        }

                                    }
                                }

                            }
                        }
                    }
                    else if (pKeyRes.StringResult == "Erase")
                    {
                        PromptKeywordOptions pEaraseOpts = new PromptKeywordOptions("");
                        pEaraseOpts.Message = "\nErase source object after offsetting? [Yes/No] ";
                        pEaraseOpts.Keywords.Add("Yes");
                        pEaraseOpts.Keywords.Add("No");
                        pEaraseOpts.Keywords.Default = "No";
                        pEaraseOpts.AllowNone = false;
                        //Get KeyWord
                        PromptResult pRes = ed.GetKeywords(pEaraseOpts);

                        //If Get Keyword seccess
                        if (pRes.Status == PromptStatus.OK)
                        {

                            PromptPointOptions op = new PromptPointOptions("\nSpecify through point:");
                            op.AllowArbitraryInput = true;
                            PromptPointResult pEarase = ed.GetPoint(op);
                            if (pEarase.Status == PromptStatus.OK)
                            {
                                using (Transaction tr = db.TransactionManager.StartTransaction())
                                {
                                    Curve ent = tr.GetObject(singleId[0], OpenMode.ForWrite) as Curve;
                                    Point3d pickpoint = pEarase.Value;
                                    distance = ent.GetClosestPointTo(pickpoint, true).DistanceTo(pickpoint);
                                    DBObjectCollection Neoffsetcurs = ent.GetOffsetCurves(distance);
                                    DBObjectCollection Posoffsetcurs = ent.GetOffsetCurves(-distance);

                                    foreach (Entity acEnt in Neoffsetcurs)
                                    {
                                        AppendEntity(acEnt);
                                    }
                                    if (ent is Line)
                                    {
                                        foreach (Entity acEnt in Posoffsetcurs)
                                        {
                                            AppendEntity(acEnt);
                                        }
                                    }
                                    if (pRes.StringResult == "Yes")
                                    {
                                        DeleteEntity(ent.ObjectId);
                                    }
                                    tr.Commit();
                                }
                            }
                        }
                    }
                    else if (pKeyRes.StringResult.Contains("Layer"))
                    {

                        PromptKeywordOptions pLayerOpts = new PromptKeywordOptions("");
                        pLayerOpts.Message = "\nEnter layer option for offset objects [Current/Source]";
                        pLayerOpts.Keywords.Add("Current");
                        pLayerOpts.Keywords.Add("Source");
                        pLayerOpts.Keywords.Default = "Current";
                        pKeyOpts.AllowNone = false;
                        //Get KeyWord
                        PromptResult pLayRes = ed.GetKeywords(pLayerOpts);
                        //If Get Keyword seccess
                        if (pLayRes.Status == PromptStatus.OK)
                        {

                            PromptPointOptions op = new PromptPointOptions("\nSpecify through point:");
                            op.AllowArbitraryInput = true;
                            PromptPointResult rlay = ed.GetPoint(op);
                            if (rlay.Status == PromptStatus.OK)
                            {
                                using (Transaction tr = db.TransactionManager.StartTransaction())
                                {
                                    Curve ent = tr.GetObject(singleId[0], OpenMode.ForWrite) as Curve;
                                    Point3d pickpoint = rlay.Value;
                                    distance = ent.GetClosestPointTo(pickpoint, true).DistanceTo(pickpoint);
                                    DBObjectCollection Neoffsetcurs = ent.GetOffsetCurves(distance);
                                    DBObjectCollection Posoffsetcurs = ent.GetOffsetCurves(-distance);

                                    foreach (Entity acEnt in Neoffsetcurs)
                                    {
                                        if (pLayRes.StringResult == "Source")
                                        {
                                            acEnt.Layer = ent.Layer;
                                        }
                                        AppendEntity(acEnt);
                                    }
                                    if (ent is Line)
                                    {
                                        foreach (Entity acEnt in Posoffsetcurs)
                                        {
                                            if (pLayRes.StringResult == "Source")
                                            {
                                                acEnt.Layer = ent.Layer;
                                            }
                                            AppendEntity(acEnt);
                                        }
                                    }
                                    tr.Commit();
                                }
                            }
                        }
                    }
                }
               
            }   
            }
        [CommandMethod("DTCurver")]
        public static void JigTextAlonPpolyline()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var entityOptions = new PromptEntityOptions("\nSelect Entity: ");

            var entityResult = ed.GetEntity(entityOptions);
            if (entityResult.Status != PromptStatus.OK)
                return;

            var stringOptions = new PromptStringOptions("\nEnter a text: ");
            stringOptions.AllowSpaces = true;
            var stringResult = ed.GetString(stringOptions);
            if (stringResult.Status != PromptStatus.OK)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pline = (Curve)tr.GetObject(entityResult.ObjectId, OpenMode.ForRead);
                using (var text = new DBText())
                {
                    text.SetDatabaseDefaults();
                    text.Normal = -Vector3d.ZAxis;
                    text.Justify = AttachmentPoint.BottomCenter;
                    text.AlignmentPoint = Point3d.Origin;
                    text.TextString = stringResult.StringResult;

                    var jig = new TextJig(text, pline);
                    var result = ed.Drag(jig);
                    if (result.Status == PromptStatus.OK)
                    {
                        var currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        currentSpace.AppendEntity(text);
                        tr.AddNewlyCreatedDBObject(text, true);
                    }
                }
                tr.Commit();
            }
        }
    }
    

    class TextJig : EntityJig
    {
        DBText text;
        Curve curver;
        Point3d dragPt;
        Plane plane;
        Database db;

        public TextJig(DBText text, Curve curver) : base(text)
        {
            this.text = text;
            this.curver = curver;
            plane = new Plane(Point3d.Origin, -Vector3d.ZAxis);
            db = HostApplicationServices.WorkingDatabase;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions("\nSpecicfy insertion point or [Mirror/Style Setting/Background Mask] ", "Mirror Style Background");

            options.UserInputControls =
                UserInputControls.Accept3dCoordinates |
                UserInputControls.UseBasePointElevation;
            var result = prompts.AcquirePoint(options);
            if (result.Status == PromptStatus.Keyword)
            {
                if (result.StringResult == "Mirror")
                {

                }
                else if (result.StringResult == "Style")
                {

                }
                else if (result.StringResult== "Background")
                {

                }
            }
            if (result.Value.IsEqualTo(dragPt))
                return SamplerStatus.NoChange;
            dragPt = result.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            var point = curver.GetClosestPointTo(dragPt, false);
            var angle = curver.GetFirstDerivative(point).AngleOnPlane(plane);
            text.AlignmentPoint = point;
            text.Rotation = angle;
            text.AdjustAlignment(db);
            return true;
        }
    }
}
