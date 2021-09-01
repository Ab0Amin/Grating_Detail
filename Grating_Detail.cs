// Decompiled with JetBrains decompiler
// Type: Grating_Detail
// Assembly: Grating_Detail, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C85C8E2-865E-400F-B121-DAE359F05EC3
// Assembly location: C:\Program Files\Tekla Structures\20.1\nt\bin\plugins\Tekla\Model\Grating_Detail.dll

using System;
using System.Collections;
using System.Collections.Generic;
using Tekla.Structures;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Plugins;
using Tekla.Structures.Solid;
using System.Windows.Forms;

[Plugin("Grating_Detail")]
[PluginUserInterface(UserInterfaceDefinitions.Plugin3)]
public class Grating_Detail : PluginBase
{
  private readonly StructuresData3 data;
  private readonly Tekla.Structures.Model.Model myModel;

  public Grating_Detail(StructuresData3 data)
  {
    this.data = data;
    this.myModel = new Tekla.Structures.Model.Model();
    this.myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
  }

  public override List<PluginBase.InputDefinition> DefineInput()
  {
    List<PluginBase.InputDefinition> inputDefinitionList = new List<PluginBase.InputDefinition>();
    Picker picker = new Picker();
    ModelObjectEnumerator objectEnumerator = picker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS);
    Point point = picker.PickPoint();
    ArrayList _Input = new ArrayList();
    while (objectEnumerator.MoveNext())
    {
      Part current = objectEnumerator.Current as Part;
      Identifier identifier = current.Identifier;
      if (current != null)
        _Input.Add((object) identifier);
    }
    PluginBase.InputDefinition inputDefinition1 = new PluginBase.InputDefinition(_Input);
    PluginBase.InputDefinition inputDefinition2 = new PluginBase.InputDefinition(point);
    inputDefinitionList.Add(inputDefinition1);
    inputDefinitionList.Add(inputDefinition2);
    return inputDefinitionList;
  }

  public override bool Run(List<PluginBase.InputDefinition> Input)
  {
    if (this.IsDefaultValue(this.data.offset))
      this.data.offset = 0.0;
    if (this.IsDefaultValue(this.data.Plate_b))
      this.data.Plate_b = 100.0;
    if (this.IsDefaultValue(this.data.Plate_h))
      this.data.Plate_h = 6.0;
    if (this.IsDefaultValue(this.data.Pos_1))
      this.data.Pos_1 = "W";
    if (this.IsDefaultValue(this.data.Plate_pos2))
      this.data.Plate_pos2 = 20;
    if (this.IsDefaultValue(this.data.Plate_matrial))
      this.data.Plate_matrial = "A36";
    if (this.IsDefaultValue(this.data.Plate_name))
      this.data.Plate_name = "Plate";
    if (this.IsDefaultValue(this.data.holeDiam))
      this.data.holeDiam = 500.0;
    if (this.IsDefaultValue(this.data.hole_L))
      this.data.hole_L = 500.0;
    if (this.IsDefaultValue(this.data.hole_w))
      this.data.hole_w = 500.0;
    if (this.IsDefaultValue(this.data.weld_aboveline))
      this.data.weld_aboveline = 6.0;
    if (this.IsDefaultValue(this.data.weld_belowline))
      this.data.weld_belowline = 6.0;
    if (this.IsDefaultValue(this.data.holeType))
      this.data.holeType = 0;
    ArrayList input = (ArrayList) Input[0].GetInput();
    
      try 
	{
        this.createconnection((Point)Input[1].GetInput(), input);
	}
	
   catch (Exception e)
        {
            MessageBox.Show(e.ToString());
        }
      return true;
  }

  public void createconnection(Point center_point, ArrayList Gratting_assembly_arraylist)
  {
    foreach (Identifier ID in Gratting_assembly_arraylist)
    {
      Part part1 = (Part) this.myModel.SelectModelObject(ID);
      this.myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
      this.Get_Lower_face_edge(part1);
      string Partprofile1;
      string Partprofile2;
      if (this.data.holeType == 0)
      {
        Partprofile1 = "ROD" + (this.data.holeDiam + 2 * this.data.Plate_h).ToString();
        Partprofile2 = "ROD" + (this.data.holeDiam +  this.data.Plate_h).ToString();
      }
      else
      {
          Partprofile1 = (this.data.hole_L + 2 * this.data.Plate_h).ToString() + "*" + (2 * this.data.Plate_h + this.data.hole_w).ToString();
        Partprofile2 = Partprofile1;
      }
     // assembly.SetMainPart(part1);
      Beam cut = CreateCut(center_point, part1, Partprofile2);
     
        Assembly assembly = part1.GetAssembly();
        ArrayList secondaries = assembly.GetSecondaries();
        ArrayList newSecondaries = new ArrayList();
      for (int index = 0; index < secondaries.Count; ++index)
      {
          Part Part_To_Be_Cut = (Part)secondaries[index];

          if (Part_To_Be_Cut.GetType().Name == "PolyBeam")
        {
            secondaries.Remove(Part_To_Be_Cut);
            if (index >= 0)
            {
                --index;
                continue;
            }
            else if (secondaries.Count == 0)
            {
                index = -1;
                continue;
            }

        }

          else if (Part_To_Be_Cut.Class == "2222")
          {
              secondaries.Remove(Part_To_Be_Cut);
              if (index >= 0)
              {
                  --index;
                  continue;
              }
              else if (secondaries.Count == 0)
              {
                  index = -1;
                  continue;
              }

          }

          if (Part_To_Be_Cut.Class != "211" && Part_To_Be_Cut.Profile.ProfileString.Contains("PL"))
          {
              // split into two side plates 
              bool intersected = checkIntersectionSidePlates(cut, Part_To_Be_Cut);

              if (intersected)
              {
                  Beam partToCutBeam = secondaries[index] as Beam;
                  bool need = needSidePlate(partToCutBeam, center_point, center_point);
                  CreateCut(center_point, Part_To_Be_Cut, Partprofile1).Delete();
                 
                  int factor = 1;
                  CutPlane cut1 = cutsideplate(center_point, Part_To_Be_Cut, factor);
                  double length = 100;
                  Part_To_Be_Cut.GetReportProperty("LENGTH_NET", ref length);
                  if (length < 1)
                  {
                      factor = -1;
                      cut1.Delete();
                      cut1 = cutsideplate(center_point, Part_To_Be_Cut, factor);


                  }
                 
                  if (need)
                  {
                      Beam SecSidePlate = copyBeam(partToCutBeam);
                      CreateCut(center_point, SecSidePlate, Partprofile1).Delete();
                      CutPlane cut2 = cutsideplate(center_point, SecSidePlate, -1 * factor);
                      insert_weld(part1, SecSidePlate);
                      newSecondaries.Add(SecSidePlate);
                  }
                  else
                  {
                  }
              }

          }

      }
      // split reget sec parts
      //secondaries.Clear();
      //assembly = part1.GetAssembly();
      //secondaries = assembly.GetSecondaries();
     
        //Beam cut11 = this.CreateCut(center_point, part1, Partprofile1);
      OBB obb1 = this.CreateObb((Part) cut);
      List<LineSegment> lowerFaceEdge = this.Get_Lower_face_edge(part1);
      List<Beam> beamList = new List<Beam>();
      List<LineSegment> lineList = new List<LineSegment>();
      List<LineSegment> lineSegmentList = new List<LineSegment>();
#region circle
      if (this.data.holeType == 0)
  {
        Point point1 = new Point();
        Point point2 = new Point();
        foreach (LineSegment lineSegment in lowerFaceEdge)
        {
          if (obb1.Intersects(lineSegment))
          {
            LineSegment obb2 = Intersection.LineSegmentToObb(lineSegment, obb1);
            lineSegmentList.Add(obb2);
          }
        }
        Point point1_1 = lineSegmentList[(int)(lineSegmentList.Count / 3)].Point1;
        List<LineSegment> lowerEdge = this.new_get_lower_edge(part1);
        ArrayList arrayList = new ArrayList();
        List<Point> pointList = new List<Point>();
        Tekla.Structures.Model.Solid solid = cut.GetSolid();
        foreach (LineSegment line in lowerEdge)
        {
          if ( solid.Intersect(line).Count > 0)
          {
              foreach (Point obj in solid.Intersect(line))
                  pointList.Add(obj);
          }
        }
        Point P1 = pointList[0];
        Point P2 = pointList[pointList.Count - 1];
        if (point1_1 == P1 || point1_1 == P2)
          point1_1 = lineSegmentList[lineSegmentList.Count / 2 - 1].Point1;

        if (point1_1 == P1 || point1_1 == P2)
            point1_1 = lineSegmentList[lineSegmentList.Count / 2 - 2].Point1;
          PolyBeam polyBeam = polyToePlate(point1_1, P1, P2);
       //s
       //   Beam cut2 = this.CreateCut(center_point, part1, Partprofile2);
        this.insert_weld(part1, (Part) polyBeam);
        cut.Delete();
        //cut2.Delete();
        ModelObjectEnumerator booleans = polyBeam.GetBooleans();
        while (booleans.MoveNext())
          (booleans.Current as BooleanPart).Delete();
       
          CreateCut(center_point, part1, Partprofile1).Delete();

      }     
#endregion
      #region rectangular
      else
      {
          CreateCut(center_point, part1, Partprofile1).Delete();

          // lines for  toe plates
          foreach (LineSegment lineSegment in lowerFaceEdge)
          {
              if (obb1.Intersects(lineSegment))
              {
                  try
                  {
                      LineSegment obb2 = Intersection.LineSegmentToObb(lineSegment, obb1);
                      if (obb2.Length() > 1) lineList.Add(obb2);
                  }
                  catch (Exception)
                  {
                  }
              }
          }
          cut.Delete();

          // remove toe plates from sec
          for (int index = 0; index < secondaries.Count; ++index)
          {
              Beam beam = secondaries[index] as Beam;
              PolyBeam polybeam = secondaries[index] as PolyBeam;
              if (polybeam != null)
              {
                  secondaries.Remove(polybeam);
                  index = -1;
              }
              else if (beam != null)
              {
                  if (beam.Class == "211")
                  {
                      secondaries.Remove(beam);
                      index = -1;
                  }
              }
          }
          //for (int i = 0; i < newSecondaries.Count; i++)
          //{
          //    secondaries.Insert(i, newSecondaries);

          //}
          // small toeplate points
          //foreach (Part GPart in secondaries)
          //{
          //    List<LineSegment> lowerlines = Get_Lower_face_edge(GPart);

          //    foreach (LineSegment lineSegment in lowerlines)
          //    {
          //        if (obb1.Intersects(lineSegment))
          //        {
          //            try
          //            {
          //                LineSegment obb2 = Intersection.LineSegmentToObb(lineSegment, obb1);
          //                if (obb2.Length() > 1) lineList.Add(obb2);
          //            }
          //            catch (Exception)
          //            {
          //            }
          //        }
          //    }
          //}

          //foreach (Part GPart in newSecondaries)
          //{
          //    List<LineSegment> lowerlines = Get_Lower_face_edge(GPart);

          //    foreach (LineSegment lineSegment in lowerlines)
          //    {
          //        if (obb1.Intersects(lineSegment))
          //        {
          //            try
          //            {
          //                LineSegment obb2 = Intersection.LineSegmentToObb(lineSegment, obb1);
          //                if (obb2.Length() > 1) lineList.Add(obb2);
          //            }
          //            catch (Exception)
          //            {
          //            }
          //        }
          //    }
          //}

          //compine lines
          //for (int index1 = 0; index1 <= lineList.Count - 1; ++index1)
          //{
          //    LineSegment line1 = lineList[index1];
          //    for (int index2 = index1 + 1; index2 <= lineList.Count - 1; ++index2)
          //    {
          //        LineSegment line2 = lineList[index2];
          //        if (line1 != line2 && line1 != null && line2 != null)
          //        {
          //            bool done = false;
          //            LineSegment newline;
          //            combine_beams_have_same_vector(line1, line2, out done, out newline);
          //            if (done)
          //            {
          //                lineList.Remove(line1);
          //                lineList.Remove(line2);
          //                lineList.Add(newline);
          //                --index1;
          //                break;

          //            }

          //        }
          //    }
          //}


          // insert toeplates
          for (int i = 0; i < lineList.Count; i++)
          {
              Beam beam = this.insert_toeplate(lineList[i].Point1, lineList[i].Point2);
              insert_weld(part1, beam);
              beamList.Add(beam);
          }

          // cut part by part
          for (int index = 0; index < beamList.Count; ++index)
          {
              try
              {
                  this.part_cut((Part)beamList[index], (Part)beamList[index + 1]);
              }
              catch (Exception)
              {
              }
          }
          try
          {
              this.part_cut((Part)beamList[beamList.Count - 1], (Part)beamList[0]);
          }
          catch (Exception)
          {
          }



      }
      #endregion
    }
  }

  private PolyBeam polyToePlate(Point point1_1, Point P1, Point P2)
  {
      Chamfer C = new Chamfer();
      C.Type = Chamfer.ChamferTypeEnum.CHAMFER_ARC_POINT;
      PolyBeam polyBeam = new PolyBeam();
      polyBeam.Profile.ProfileString = "PL" + this.data.Plate_b.ToString() + "*" + this.data.Plate_h.ToString();
      polyBeam.Position.Depth = Position.DepthEnum.FRONT;
      polyBeam.Position.Plane = Position.PlaneEnum.MIDDLE;
      polyBeam.Position.Rotation = Position.RotationEnum.TOP;
      polyBeam.Position.DepthOffset = this.data.offset;
      polyBeam.Material.MaterialString = this.data.Plate_matrial;
      polyBeam.PartNumber.Prefix = this.data.Pos_1;
      polyBeam.PartNumber.StartNumber = this.data.Plate_pos2;
      polyBeam.Name = this.data.Plate_name;
      polyBeam.AddContourPoint(new ContourPoint(P1, (Chamfer)null));
      polyBeam.AddContourPoint(new ContourPoint(point1_1, C));
      polyBeam.AddContourPoint(new ContourPoint(P2, (Chamfer)null));
      polyBeam.Insert();
      return polyBeam;
  }


  public bool needSidePlate(Beam sidePlate, Point p1, Point p2)
  {
      bool need = false;
      bool check1 = false, check2 = false;
      double length1 = 10, length2 = 10;
      sidePlate.GetReportProperty("LENGTH_NET", ref length1);
      CutPlane cut1 = cutsideplate(p1, sidePlate, 1);
      CutPlane cut2 = cutsideplate(p2, sidePlate, 1);
      sidePlate.GetReportProperty("LENGTH_NET", ref length1);
      if (length1 > 1)
      {
          check1 = true;
      }
      cut1.Delete();
      cut2.Delete();
      sidePlate.GetReportProperty("LENGTH_NET", ref length1);
      CutPlane cut3 = cutsideplate(p1, sidePlate, -1);
      CutPlane cut4 = cutsideplate(p2, sidePlate, -1);

      sidePlate.GetReportProperty("LENGTH_NET", ref length2);
      cut3.Delete();
      cut4.Delete();
      if (length2 > 1)
      {
          check2 = true;
      }
      if (check1 == true && check2 == true)
      {
          need = true;
      }
      return need;
  }
  private static Beam copyBeam(Beam partToCutBeam)
  {
      Beam SecSidePlate = new Beam();
      SecSidePlate.Profile = partToCutBeam.Profile;
      SecSidePlate.Material = partToCutBeam.Material;
      SecSidePlate.Position = partToCutBeam.Position;
      SecSidePlate.AssemblyNumber = partToCutBeam.AssemblyNumber;
      SecSidePlate.PartNumber = partToCutBeam.PartNumber;
      SecSidePlate.StartPoint = partToCutBeam.StartPoint;
      SecSidePlate.EndPoint = partToCutBeam.EndPoint;
      SecSidePlate.Class = "2222";
      SecSidePlate.Insert();
      return SecSidePlate;
  }

  private static CutPlane cutsideplate(Point point1, Part Part_To_Be_Cut, int factor)
  {
      CoordinateSystem coordinate = Part_To_Be_Cut.GetCoordinateSystem();
      Vector vecX = coordinate.AxisX;
      Vector vecY = coordinate.AxisY;
      Vector vecZ = vecY.Cross(vecX);
      vecZ.Normalize();
      vecX.Normalize();
      vecY.Normalize();
      CutPlane cutlinePlane = new CutPlane();
      cutlinePlane.Plane = new Plane();
      cutlinePlane.Plane.Origin = point1;
      cutlinePlane.Plane.AxisX = vecY * factor;
      cutlinePlane.Plane.AxisY = vecZ;
      cutlinePlane.Father = Part_To_Be_Cut;
      cutlinePlane.Insert();
      return cutlinePlane;
  }

  private static bool checkIntersectionSidePlates(Beam cut, Part Part_To_Be_Cut)
  {
      Solid cutSplid = cut.GetSolid();
      ArrayList ppts = Part_To_Be_Cut.GetCenterLine(false);
      LineSegment ll = new LineSegment(ppts[0] as Point, ppts[1] as Point);
      ArrayList intersectedppts = cutSplid.Intersect(ll);
      if (intersectedppts.Count > 0)
      {
          return true;
      }
      else
      {
          return false;
      }
  }

  public void maxAndMinY(List<LineSegment> lineSegments, out Point point_max, out Point point_min)
  {
      Point point1 = new Point();
      Point point2 = new Point();
      List<Point> pointList = new List<Point>();
      foreach (LineSegment lineSegment in lineSegments)
      {
          pointList.Add(lineSegment.Point1);
          pointList.Add(lineSegment.Point2);
      }
      double num1 = 0.0;
      double num2 = 100000000.0;
      foreach (Point point3 in pointList)
      {
          if (point3.Y > num1)
          {
              num1 = point3.Y;
              point1 = point3;
          }
          if (point3.Y < num2)
          {
              num2 = point3.Y;
              point2 = point3;
          }
      }
      point_max = point1;
      point_min = point2;
  }

  public Beam check_with_beam(Point start_point, Point end_point)
  {
      Beam beam = new Beam();
      beam.StartPoint = start_point;
      beam.EndPoint = end_point;
      beam.Profile.ProfileString = "ROD100";
      beam.Position.Depth = Position.DepthEnum.FRONT;
      beam.Position.Plane = Position.PlaneEnum.RIGHT;
      beam.Position.Rotation = Position.RotationEnum.TOP;
      beam.Insert();
      return beam;
  }

  public Point CallulateCenterPoint(Point minpoint, Point maxpoint)
  {
      return new Point(minpoint.X + (maxpoint.X - minpoint.X) / 2.0, minpoint.Y + (maxpoint.Y - minpoint.Y) / 2.0, minpoint.Z + (maxpoint.Z - minpoint.Z) / 2.0);
  }

  public ContourPlate CreateCut(
    Point point1,
    Point point2,
    Point point3,
    Point point4,
    Part Part_To_Be_Cut)
  {
      ContourPlate contourPlate = new ContourPlate();
      contourPlate.Profile.ProfileString = "PL500";
      contourPlate.Material.MaterialString = "ANTIMATERIAL";
      contourPlate.Finish = "";
      contourPlate.Class = "BlOpCl";
      contourPlate.Position.Depth = Position.DepthEnum.MIDDLE;
      contourPlate.AddContourPoint(new ContourPoint(point1, (Chamfer)null));
      contourPlate.AddContourPoint(new ContourPoint(point2, (Chamfer)null));
      contourPlate.AddContourPoint(new ContourPoint(point3, (Chamfer)null));
      contourPlate.AddContourPoint(new ContourPoint(point4, (Chamfer)null));
      contourPlate.Insert();
      BooleanPart booleanPart = new BooleanPart();
      booleanPart.Father = (ModelObject)Part_To_Be_Cut;
      booleanPart.OperativePart = (Part)contourPlate;
      booleanPart.Insert();
      return contourPlate;
  }

  public Beam CreateCut(Point point1, Part Part_To_Be_Cut, string Partprofile)
  {
      Beam beam = new Beam();
      beam.Profile.ProfileString = Partprofile;
      beam.Material.MaterialString = "ANTIMATERIAL";
      beam.Finish = "";
      beam.Class = "BlOpCl";
      beam.Position.Depth = Position.DepthEnum.MIDDLE;
      beam.Position.Rotation = Position.RotationEnum.TOP;
      beam.Position.Plane = Position.PlaneEnum.MIDDLE;
      double num = 0.0;
      Part_To_Be_Cut.GetReportProperty("END_Z", ref num);
      beam.StartPoint = new Point(point1.X, point1.Y, num + 500.0);
      beam.EndPoint = new Point(point1.X, point1.Y, num - 500.0);
      beam.Insert();
      BooleanPart booleanPart = new BooleanPart();
      booleanPart.Father = (ModelObject)Part_To_Be_Cut;
      booleanPart.OperativePart = (Part)beam;
      booleanPart.Insert();
      return beam;
  }

  public List<LineSegment> Get_Lower_face_edge(Part GPart)
  {
      this.myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
      Solid solid = GPart.GetSolid(Solid.SolidCreationTypeEnum.NORMAL);
      //GPart.Class = "2";
      //GPart.Modify();
      List<LineSegment> lineSegmentList = new List<LineSegment>();
      if (solid != null)
      {
          EdgeEnumerator edgeEnumerator = solid.GetEdgeEnumerator();

          while (edgeEnumerator.MoveNext())
          {
              Edge current = edgeEnumerator.Current as Edge;
              LineSegment lineSegment = new LineSegment(current.StartPoint, current.EndPoint);
              if (Math.Abs(lineSegment.Point1.Z - lineSegment.Point2.Z) < 1.0)
                  lineSegmentList.Add(lineSegment);
          }
          for (int index1 = 0; index1 < lineSegmentList.Count; ++index1)
          {
              LineSegment lineSegment1 = lineSegmentList[index1];
              for (int index2 = 0; index2 < lineSegmentList.Count; ++index2)
              {
                  LineSegment lineSegment2 = lineSegmentList[index2];
                  if (Math.Abs(lineSegment1.Point1.X - lineSegment2.Point1.X) < 1.0 && Math.Abs(lineSegment1.Point1.Y - lineSegment2.Point1.Y) < 1.0 && lineSegment1.Point1.Z != lineSegment2.Point1.Z)
                  {
                      if (lineSegment2.Point1.Z > lineSegment1.Point1.Z)
                          lineSegmentList.Remove(lineSegment2);
                      else
                          lineSegmentList.Remove(lineSegment1);
                  }
              }
          }
          for (int index1 = 0; index1 < lineSegmentList.Count; ++index1)
          {
              LineSegment lineSegment1 = lineSegmentList[index1];
              for (int index2 = 0; index2 < lineSegmentList.Count; ++index2)
              {
                  LineSegment lineSegment2 = lineSegmentList[index2];
                  Point point1 = lineSegment2.Point1;
                  LineSegment lineSegment3 = new LineSegment(lineSegment2.Point2, point1);
                  if (Math.Abs(lineSegment1.Point1.X - lineSegment3.Point1.X) < 1.0 && Math.Abs(lineSegment1.Point1.Y - lineSegment3.Point1.Y) < 1.0 && lineSegment1.Point1.Z != lineSegment3.Point1.Z)
                  {
                      if (lineSegment3.Point1.Z > lineSegment1.Point1.Z)
                          lineSegmentList.Remove(lineSegment2);
                      else
                          lineSegmentList.Remove(lineSegment1);
                  }
              }
          }
      }
      return lineSegmentList;
  }

  public List<LineSegment> new_get_lower_edge(Part Gpart)
  {
      List<LineSegment> lineSegmentList = new List<LineSegment>();
      Point point1 = new Point();
      Point point2 = new Point();
      Point point3 = new Point();
      Point point4 = new Point();
      Point point5 = new Point();
      Tekla.Structures.Model.Solid solid = Gpart.GetSolid(Tekla.Structures.Model.Solid.SolidCreationTypeEnum.PLANECUTTED);
      EdgeEnumerator edgeEnumerator = solid.GetEdgeEnumerator();
      Point maximumPoint = solid.MaximumPoint;
      Point minimumPoint = solid.MinimumPoint;
      TransformationPlane transformationPlane = this.myModel.GetWorkPlaneHandler().GetCurrentTransformationPlane();
      Point point6 = transformationPlane.TransformationMatrixToGlobal.Transform(maximumPoint);
      Point point7 = transformationPlane.TransformationMatrixToGlobal.Transform(minimumPoint);
      if (point6.Z > point7.Z)
          point3 = minimumPoint;
      else if (point6.Z < point7.Z)
          point3 = maximumPoint;
      if (solid != null)
      {
          while (edgeEnumerator.MoveNext())
          {
              Edge current = edgeEnumerator.Current as Edge;
              if (current.StartPoint.Z - current.EndPoint.Z == 0.0 && current.StartPoint.Z == point3.Z)
              {
                  LineSegment lineSegment = new LineSegment(current.StartPoint, current.EndPoint);
                  lineSegmentList.Add(lineSegment);
              }
          }
      }
      return lineSegmentList;
  }

  public Weld insert_weld(Part Main_part, Part Secandary_part)
  {
      Weld weld = new Weld();
      weld.MainObject = (ModelObject)Main_part;
      weld.SecondaryObject = (ModelObject)Secandary_part;
      weld.SizeAbove = this.data.weld_aboveline;
      weld.TypeAbove = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET;
      weld.SizeBelow = this.data.weld_belowline;
      weld.TypeBelow = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET;
      weld.ShopWeld = true;
      weld.AroundWeld = false;
      CoordinateSystem coordinateSystem = Secandary_part.GetCoordinateSystem();
      weld.Direction = coordinateSystem.AxisY;
      weld.Insert();
      return weld;
  }

  public Beam insert_toeplate(Point start_point, Point end_point)
  {
      Beam beam = new Beam();
      beam.StartPoint = start_point;
      beam.EndPoint = end_point;
      beam.Profile.ProfileString = "PL" + this.data.Plate_b.ToString() + "*" + this.data.Plate_h.ToString();
      beam.Position.Depth = Position.DepthEnum.FRONT;
      beam.Position.Plane = Position.PlaneEnum.RIGHT;
      beam.Position.Rotation = Position.RotationEnum.TOP;
      beam.Position.DepthOffset = this.data.offset;
      beam.Material.MaterialString = this.data.Plate_matrial;
      beam.PartNumber.Prefix = this.data.Pos_1;
      beam.PartNumber.StartNumber = this.data.Plate_pos2;
      beam.Name = this.data.Plate_name;
      beam.Class = "211";
      beam.Insert();
      return beam;
  }

  public void part_cut(Part part_to_becut, Part cut_part)
  {
      BooleanPart booleanPart = new BooleanPart();
      booleanPart.Father = (ModelObject)part_to_becut;
      cut_part.Class = "BlOpCl";
      booleanPart.OperativePart = cut_part;
      booleanPart.Insert();
  }

  public Vector Get_Part_Vector(Beam part)
  {
      Vector vec;
      if (part != null)
      {
          Point p1 = part.StartPoint;
          Point p2 = part.EndPoint;
          vec = new Vector(p2 - p1);
          vec.Normalize();
      }
      else
      {
          vec = new Vector();

      }
      return vec;
  }

  public void combine_beams_have_same_vector(LineSegment first_line, LineSegment sec_line, out bool done, out LineSegment compined)
  {
      done = false;
      //platee =new Beam() ;

      Vector partVector = new Vector(first_line.Point1 - first_line.Point2);
      Vector partVector2 = new Vector(sec_line.Point1 - sec_line.Point2);
      partVector.Normalize();
      partVector2.Normalize();
      if ((partVector2 == partVector))
      {

          double dis = Distance.PointToPoint(first_line.Point1, sec_line.Point2);
          if (dis > 1)
          {
              Vector Vec = new Vector(sec_line.Point2 - first_line.Point1);
              Vec.Normalize();
              //first_line.EndPoint = sec_line.EndPoint;
              //first_line.EndPoint = new Point();
              //first_line.Modify();
              compined = new LineSegment(first_line.Point1, sec_line.Point2);
              done = true;

          }
          else
          {

              //first.StartPoint = sec.StartPoint;
              //first.Modify();
              compined = new LineSegment(sec_line.Point1, first_line.Point2);

              done = true;

          }



          //if (Distance.PointToPoint(first.StartPoint, sec.EndPoint) > 1)
          //{
          // platee  = insert_toeplate(first.StartPoint, sec.EndPoint);
          // Vector Vec = this.Get_Part_Vector(platee);
          // Vec.Normalize();
          // if (Vec != partVector)
          // {
          //     platee.Delete();
          // platee  = insert_toeplate( sec.EndPoint,first.StartPoint);

          // }
          //}
          //else
          //{
          //  platee=  insert_toeplate(first.EndPoint, sec.StartPoint);
          //  Vector Vec = this.Get_Part_Vector(platee);
          //  Vec.Normalize();
          //  if (Vec != partVector)
          //  {
          //      platee.Delete();
          //      platee = insert_toeplate( sec.StartPoint,first.EndPoint);

          //  }
          //}
          //insert_weld(main, platee);
          //done = true;


      }
      else
      {
          compined = new LineSegment();
      }
      //return platee;
  }

  public OBB CreateObb(Part currentBeam)
  {
      OBB obb = (OBB)null;
      if (currentBeam != null)
      {
          WorkPlaneHandler workPlaneHandler = this.myModel.GetWorkPlaneHandler();
          this.myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
          Tekla.Structures.Model.Solid solid1 = currentBeam.GetSolid();
          Point center = this.CallulateCenterPoint(solid1.MaximumPoint, solid1.MinimumPoint);
          CoordinateSystem coordinateSystem = currentBeam.GetCoordinateSystem();
          workPlaneHandler.SetCurrentTransformationPlane(new TransformationPlane(coordinateSystem));
          Tekla.Structures.Model.Solid solid2 = currentBeam.GetSolid();
          Point maximumPoint = solid2.MaximumPoint;
          Point minimumPoint = solid2.MinimumPoint;
          double extent0 = (maximumPoint.X - minimumPoint.X) / 2.0;
          double extent1 = (maximumPoint.Y - minimumPoint.Y) / 2.0;
          double extent2 = (maximumPoint.Z - minimumPoint.Z) / 2.0;
          obb = new OBB(center, coordinateSystem.AxisX, coordinateSystem.AxisY, coordinateSystem.AxisX.Cross(coordinateSystem.AxisY), extent0, extent1, extent2);
          this.myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
      }
      return obb;
  }

  public class UserInterfaceDefinitions
  {
    public const string Plugin3 =
            "page(\"TeklaStructures\",\"\")\n   " +
            " {\n    joint(1, Grating_Detail)\n    {\n   " +
            "   tab_page(\"1\", \" Picture \", 1)\n  " +
            "      {\n          " +





              "   attribute(\"\", \"t\", label, \"%s\", none, none, \"0\", \"0\", 140, 75)\n  " +
            "          attribute(\"\", \"h\", label, \"%s\", none, none, \"0\", \"0\", 250, 75)\n   " +
            "         attribute(\"\", \"Prefix\", label, \"%s\", none, none, \"0\", \"0\", 350, 75)\n       " +
            "     attribute(\"\", \"start_NO\", label, \"%s\", none, none, \"0\", \"0\", 465, 75)\n      " +
            "      attribute(\"\", \"matrial\", label, \"%s\", none, none, \"0\", \"0\", 600, 75)\n       " +
            "     attribute(\"\", \"name\", label, \"%s\", none, none, \"0\", \"0\", 770, 75)\n        " +
            "    attribute(\"\", \"Plate\", label, \"%s\", none, none, \"0\", \"0\", 30, 100)\n      " +
            "      parameter(\"\", \"Plate_h\", distance, number, 125, 100, 40)\n       " +
            "     parameter(\"\", \"Plate_b\", distance, number, 215, 100, 70)\n       " +
            "     parameter(\"\", \"Pos_1\", string, text, 350, 100, 40)\n        " +
            "    parameter(\"\", \"Plate_pos2\", integer, number, 450, 100, 70)\n    " +
            "        parameter(\"\", \"Plate_matrial\", material, text, 580, 100, 100)\n    " +
            "        parameter(\"\", \"Plate_name\", string, text, 750, 100, 100)\n  " +





            "   attribute(\"holeType\", \"\", option, \"%s\", none, none, \"0.0\", \"0.0\", 130, 250, 200)\n  " +
            "  {\n     " +
            "   value(\"grating_hole_1.xbm\", 1)\n      " +
            "  value(\"grating_hole_2.xbm\", 0)\n  " +
            "  }\n         " +
            "  attribute(\"\", \"diam\", label, \"%s\", none, none, \"0\", \"0\", 510, 225)\n        " +
            "    parameter(\"\", \"holeDiam\", distance, number, 500, 250, 80)\n" +
            "      attribute(\"\", \"Circle\", label, \"%s\", none, none, \"0\", \"0\", 360, 250)     \n " +
            "  attribute(\"\", \"Rectangular\", label, \"%s\", none, none, \"0\", \"0\", 360, 350)        \n  " +
            "   attribute(\"\", \"W\", label, \"%s\", none, none, \"0\", \"0\", 680, 325)      \n    " +
            "     parameter(\"\", \"hole_L\", distance, number, 500, 350, 80)   \n    " +
            "  parameter(\"\", \"hole_w\", distance, number, 650, 350, 80)      \n    " +
            "    attribute(\"\", \"L\", label, \"%s\", none, none, \"0\", \"0\", 530, 325)   \n " +
            "        picture(\"13\", 0, 0, 645, 430)\n       " +
            "  parameter(\"\", \"offset\", distance, number, 550, 575, 70)\n   " +





              "   attribute(\"\", \"weld Above line\", label, \"%s\", none, none, \"0\", \"0\", 60, 480)\n     " +
            "        attribute(\"\", \"wled Below line\", label, \"%s\", none, none, \"0\", \"0\", 60, 550)\n     " +
            "        parameter(\"\", \"weld_aboveline\", distance, number, 260, 480, 100)\n            " +
            " parameter(\"\", \"weld_belowline\", distance, number, 260, 550, 100)\n   " +
            " }\n  " +


            //"    tab_page(\"2\", \" Parameters \", 2)\n " +
            //"       {\n     " +
            //"   attribute(\"\", \"THIK\", label, \"%s\", none, none, \"0\", \"0\", 122, 43)\n  " +
            //"          attribute(\"\", \"h\", label, \"%s\", none, none, \"0\", \"0\", 210, 43)\n   " +
            //"         attribute(\"\", \"Prfix\", label, \"%s\", none, none, \"0\", \"0\", 308, 43)\n       " +
            //"     attribute(\"\", \"Start_NO\", label, \"%s\", none, none, \"0\", \"0\", 406, 43)\n      " +
            //"      attribute(\"\", \"Matrial\", label, \"%s\", none, none, \"0\", \"0\", 538, 43)\n       " +
            //"     attribute(\"\", \"Name\", label, \"%s\", none, none, \"0\", \"0\", 685, 43)\n        " +
            //"    attribute(\"\", \"Plate\", label, \"%s\", none, none, \"0\", \"0\", 25, 76)\n      " +
            //"      parameter(\"\", \"Plate_h\", distance, number, 120, 76, 40)\n       " +
            //"     parameter(\"\", \"Plate_b\", distance, number, 190, 76, 70)\n       " +
            //"     parameter(\"\", \"Pos_1\", string, text, 320, 76, 40)\n        " +
            //"    parameter(\"\", \"Plate_pos2\", integer, number, 400, 76, 70)\n    " +
            //"        parameter(\"\", \"Plate_matrial\", material, text, 522, 76, 100)\n    " +
            //"        parameter(\"\", \"Plate_name\", string, text, 677, 76, 100)\n  " +
            //"  }\n     " +
           
            
            //" tab_page(\"3\", \" Welds \", 3)\n   " +
            //"     {\n      " +
            //"   attribute(\"\", \"aboveline\", label, \"%s\", none, none, \"0\", \"0\", 33, 55)\n     " +
            //"        attribute(\"\", \"belowline\", label, \"%s\", none, none, \"0\", \"0\", 36, 109)\n     " +
            //"        parameter(\"\", \"weld_aboveline\", distance, number, 165, 55, 100)\n            " +
            //" parameter(\"\", \"weld_belowline\", distance, number, 165, 109, 100)\n   " +
            //" }\n   " +
          
            
            " }\n}\n";
  }
}
