using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadsExtensions
{

    public class Extension : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var m_activeDocument = commandData.Application.ActiveUIDocument;
           // ICollection<ElementId> m_SelectElemIds;

            var m_SelectElemIds = m_activeDocument.Selection.GetElementIds();

            FamilyInstance selectedFamilyInstace = GetTheFamilyInstance(m_activeDocument);

            selectedFamilyInstace.

           
            return Result.Succeeded;
        }

        private FamilyInstance GetTheFamilyInstance(UIDocument m_activeDocument)
        {
           var  m_SelectElemIds = m_activeDocument.Selection.GetElementIds();

            if (m_SelectElemIds.Count != 0)
            {
                //List<FamilyInstance> locObjectBeam = new List<FamilyInstance>();

                foreach (ElementId id in m_SelectElemIds)
                {
                    Element o = m_activeDocument.Document.GetElement(id);

                    FamilyInstance loc_hostObject = o as FamilyInstance;
                    if (null != loc_hostObject)
                    {
                        StructuralType loc_frametype = loc_hostObject.StructuralType;

                        if (null != loc_hostObject && loc_frametype.Equals(StructuralType.Beam))
                        {
                            return loc_hostObject;
                        }
                    }
                }
            }

            return null;

        }
    }

    internal class BeamData
    {
        public double height;
        public double brethe;
        public string name;
        public StructuralType type;
        public StructuralMaterialType materialType;
        public Level level;
        public string familyName;
        public string familyType;
    }
    internal class BeamGeomentry
    {
        private FamilyInstance _familyInstance;
        public BeamGeomentry(FamilyInstance familyInstance)
        {
            _familyInstance = familyInstance;
        }

        public BeamData GetBeamData()
        {
            var beamData = new BeamData();

            beamData.name = _familyInstance.Name;
            beamData.type = _familyInstance.StructuralType;
            // beamData.brethe = _familyInstance.g
            beamData.materialType = _familyInstance.StructuralMaterialType;

            beamData.level = _familyInstance.Document.GetElement(_familyInstance.LevelId) as Level;

            beamData.familyName = _familyInstance.Symbol.FamilyName;
            beamData.familyType = _familyInstance.Name;

            return beamData;
        }

        public class CADSRebar
        {
            private Document _document;
            public CADSRebar(Document document)
            {
                _document = document;
            }
            protected List<RebarBarType> m_rebarTypes;  //a list to store all the rebar types
            protected List<RebarHookType> m_hookTypes;  //a list to store all the hook types
            protected List<RebarShape> m_shapeTypes;  //a list to store all the shape types

            protected List<Material> m_mateTypes;  //a list to store all the hook types

            protected bool GetRebarTypes()
            {
                int countStandard = 0, countStirrupTie = 0;

                // getting materials from document
                FilteredElementCollector collector = new FilteredElementCollector(_document);
                collector.OfClass(typeof(Material));
                FilteredElementIterator materialItr = collector.GetElementIterator();
                materialItr.Reset();

                List<RebarBarType> rebarTypes = GetDocumentRebarBarTypes();
                List<RebarHookType> hookTypes = GetDocumentRebarHookTypes();
                List<RebarShape> rebarShapes = GetDocumentRebarShapes();

                m_rebarTypes.Clear();
                m_hookTypes.Clear();
                m_shapeTypes.Clear();
                m_mateTypes.Clear();

                // adding steel materials to collection
                while (materialItr.MoveNext())
                {
                    Material material = materialItr.Current as Material;
                    Parameter materialClass = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_CLASS);
                    if (materialClass != null)
                    {
                        int materialClassValue = materialClass.AsInteger();
                        if (materialClassValue == (int)Autodesk.Revit.DB.Structure.StructuralMaterialType.Steel)
                            m_mateTypes.Add(material);
                    }
                }

                foreach (RebarBarType barType in rebarTypes)
                { if (null != barType) m_rebarTypes.Add(barType); }
                foreach (RebarHookType hookType in hookTypes)
                {
                    if (null != hookType)
                    {
                        m_hookTypes.Add(hookType);
                        if (this.GetHookStyle(hookType) == Autodesk.Revit.DB.Structure.RebarStyle.Standard) countStandard++;
                        if (this.GetHookStyle(hookType) == Autodesk.Revit.DB.Structure.RebarStyle.StirrupTie) countStirrupTie++;
                    }
                }
                foreach (RebarShape shapeType in rebarShapes)
                { if (null != shapeType) m_shapeTypes.Add(shapeType); }

                if (0 == m_hookTypes.Count || countStandard == 0 || countStirrupTie == 0)
                {
                    //m_Error.Add(new REXRevitException(REXError.HOOKS_NOT_EXIST));
                }
                if (0 == m_rebarTypes.Count)
                {
                   // m_Error.Add(new REXRevitException(REXError.BARS_NOT_EXIST));
                }
                if (0 == m_shapeTypes.Count)
                {
                    // m_Error.Add(new REXRevitException(REXError.SHAPE_NOT_EXIST));
                    // no error because shapes generated with CG
                }

                return true;
            }
            public List<RebarBarType> GetDocumentRebarBarTypes()
            {
                List<RebarBarType> rebarTypes = new List<RebarBarType>();

                FilteredElementIterator itor = (new FilteredElementCollector(_document)).OfClass(typeof(RebarBarType)).GetElementIterator();
                itor.Reset();
                while (itor.MoveNext())
                {
                    RebarBarType barType = itor.Current as RebarBarType;
                    if (null != barType)
                        rebarTypes.Add(barType);
                }

                return rebarTypes;
            }
            public List<RebarHookType> GetDocumentRebarHookTypes()
            {
                List<RebarHookType> hookTypes = new List<RebarHookType>();

                FilteredElementIterator itor = (new FilteredElementCollector(_document)).OfClass(typeof(RebarHookType)).GetElementIterator();
                itor.Reset();
                while (itor.MoveNext())
                {
                    RebarHookType hookType = itor.Current as RebarHookType;
                    if (null != hookType)
                        hookTypes.Add(hookType);
                }

                return hookTypes;
            }

            public List<RebarShape> GetDocumentRebarShapes()
            {
                List<RebarShape> rebarShapes = new List<RebarShape>();

                FilteredElementIterator itor = (new FilteredElementCollector(_document)).OfClass(typeof(RebarShape)).GetElementIterator();
                itor.Reset();
                while (itor.MoveNext())
                {
                    RebarShape shape = itor.Current as RebarShape;
                    if (null != shape)
                        rebarShapes.Add(shape);
                }

                return rebarShapes;
            }

            public Autodesk.Revit.DB.Structure.RebarStyle GetHookStyle(RebarHookType hookTypes)
            {
                if (null != hookTypes)
                {
                    Parameter p = hookTypes.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.REBAR_HOOK_STYLE);
                    if (null != p)
                    { if (p.AsInteger() == 1) return Autodesk.Revit.DB.Structure.RebarStyle.StirrupTie; }
                }
                return Autodesk.Revit.DB.Structure.RebarStyle.Standard;
            }
        }
    }
}
