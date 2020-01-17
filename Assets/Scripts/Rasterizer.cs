using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rasterizer 
{
    public void SetZbuffer(int id_x, int id_y, float value)
    {
        SoftwareRenderer._zBuffers[id_y * Screen.width + id_x] = value;
    }

    public float GetZbuffer(int id_x, int id_y)
    {
       return SoftwareRenderer._zBuffers[id_y * Screen.width + id_x] ;
    }

    public  void DrawPoint(int id_x, int id_y, Vector4 color)
    {
        SoftwareRenderer._pixels[id_y * Screen.width + id_x] = color;
    }

    public  void Swap(ref int a, ref int b)
    {
        int c = a;
        a = b;
        b = c;
    }

    public  void BresenhamDrawLine(Vector2 vertex1, Vector2 vertex2, Vector4 color)
    {

        //NDC to viewport transform
        int x1 = (int)vertex1.x;
        int y1 = (int)vertex1.y;
        int x2 = (int)vertex2.x;
        int y2 = (int)vertex2.y;

        //transpose line if it is too steep
        bool steep = false;
        if (Mathf.Abs(x1 - x2) < Mathf.Abs(y1 - y2))
        {
            Swap(ref x1, ref y1);
            Swap(ref x2, ref y2);
            steep = true;
        }

        //Redefine line so that it is left to right
        if (x1 > x2)
        {
            Swap(ref x1, ref x2);
            Swap(ref y1, ref y2);
        }

        //Redefined to use only int arithmetic
        int dx = x2 - x1;
        int dy = y2 - y1;
        int derror2 = Mathf.Abs(dy) * 2;
        int error2 = 0;
        int y = y1;

        for (int x = x1; x <= x2; x++)
        {
            if (steep)
            {
                DrawPoint(y, x, color);

            }
            else
            {
                DrawPoint(x, y, color);
            }
            error2 += derror2;
            if (error2 > dx)
            {
                y += (y2 > y1 ? 1 : -1);
                error2 -= dx * 2;
            }
        }
    }

    enum OutCode
    {
        INSIDE = 0,// 0000
        LEFT = 1,   // 0001
        RIGHT = 2,  // 0010
        BOTTOM = 4, // 0100
        TOP = 8,    // 1000
    }
    int xmin = 0;
    int xmax = Screen.width - 1;
    int ymin = 0;
    int ymax = Screen.height - 1;

     OutCode ComputeOutCode(double x, double y)
    {
        OutCode code;

        code = OutCode.INSIDE;          // initialised as being inside of [[clip window]]

        if (x < xmin)           // to the left of clip window
            code = OutCode.LEFT;
        else if (x > xmax)      // to the right of clip window
            code = OutCode.RIGHT;
        if (y < ymin)           // below the clip window
            code = OutCode.BOTTOM;
        else if (y > ymax)      // above the clip window
            code = OutCode.TOP;

        return code;
    }

    public void DrawWireFrame(List<Vector4> vector4s,bool isDrawPoint =false) {
        Vector4[] colors = new Vector4[3] { new Vector4(0, 1, 1, 1), new Vector4(0, 1, 1, 1), new Vector4(0, 1, 1, 1) };

        if (isDrawPoint) {
            for (int i = 0; i < 3; i++) {
                if (ComputeOutCode(vector4s[i].x, vector4s[i].y) == OutCode.INSIDE)
                {
                    DrawPoint((int)vector4s[i].x, (int)vector4s[i].y, colors[i]);
                }
            }
        }
        else {
            CohenSutherlandLineClipAndDraw(vector4s[0], vector4s[1], colors[0]);
            CohenSutherlandLineClipAndDraw(vector4s[1], vector4s[2], colors[1]);
            CohenSutherlandLineClipAndDraw(vector4s[2], vector4s[0], colors[2]);
        }
    }
    // Cohen–Sutherland clipping algorithm clips a line from
    // P0 = (x0, y0) to P1 = (x1, y1) against a rectangle with 
    // diagonal from (xmin, ymin) to (xmax, ymax).
    public void CohenSutherlandLineClipAndDraw(Vector4 point1, Vector4 point2,Vector4 color)
    {
        double x0 = point1.x;
        double y0 = point1.y;
        double x1 = point2.x;
        double y1 = point2.y;

        // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
        OutCode outcode0 = ComputeOutCode(x0, y0);
        OutCode outcode1 = ComputeOutCode(x1, y1);
        bool accept = false;

        while (true)
        {
            //if (!(outcode0 | outcode1))
            if (outcode0 == OutCode.INSIDE && outcode1 == OutCode.INSIDE)
            {
                // bitwise OR is 0: both points inside window; trivially accept and exit loop
                accept = true;
                break;
            }
            else if (outcode0 == outcode1)
            {
                // bitwise AND is not 0: both points share an outside zone (LEFT, RIGHT, TOP,
                // or BOTTOM), so both must be outside window; exit loop (accept is false)
                break;
            }
            else
            {
                // failed both tests, so calculate the line segment to clip
                // from an outside point to an intersection with clip edge
                double x = 0;
                double y = 0;

                // At least one endpoint is outside the clip rectangle; pick it.
                OutCode outcodeOut = outcode0 == OutCode.INSIDE ? outcode1 : outcode0;

                // Now find the intersection point;
                // use formulas:
                //   slope = (y1 - y0) / (x1 - x0)
                //   x = x0 + (1 / slope) * (ym - y0), where ym is ymin or ymax
                //   y = y0 + slope * (xm - x0), where xm is xmin or xmax
                // No need to worry about divide-by-zero because, in each case, the
                // outcode bit being tested guarantees the denominator is non-zero
                if (outcodeOut == OutCode.TOP)
                {           // point is above the clip window
                    x = x0 + (x1 - x0) * (ymax - y0) / (y1 - y0);
                    y = ymax;
                }
                else if (outcodeOut == OutCode.BOTTOM)
                { // point is below the clip window
                    x = x0 + (x1 - x0) * (ymin - y0) / (y1 - y0);
                    y = ymin;
                }
                else if (outcodeOut == OutCode.RIGHT)
                {  // point is to the right of clip window
                    y = y0 + (y1 - y0) * (xmax - x0) / (x1 - x0);
                    x = xmax;
                }
                else if (outcodeOut == OutCode.LEFT)
                {   // point is to the left of clip window
                    y = y0 + (y1 - y0) * (xmin - x0) / (x1 - x0);
                    x = xmin;
                }

                // Now we move outside point to intersection point to clip
                // and get ready for next pass.
                if (outcodeOut == outcode0)
                {
                    x0 = x;
                    y0 = y;
                    outcode0 = ComputeOutCode(x0, y0);
                }
                else
                {
                    x1 = x;
                    y1 = y;
                    outcode1 = ComputeOutCode(x1, y1);
                }
            }
        }
        if (accept)
        {
            // Following functions are left for implementation by user based on
            // their platform (OpenGL/graphics.h etc.)

            //DrawRectangle(xmin, ymin, xmax, ymax);
            //LineSegment(x0, y0, x1, y1);

            Vector4 clipPoint1 = new Vector4((float)x0, (float)y0, point1.z, point1.w);
            Vector4 clipPoint2 = new Vector4((float)x1, (float)y1, point1.z, point1.w);
            BresenhamDrawLine(clipPoint1, clipPoint2, color);
        }
    }

    public float Edge(Vector3 a, Vector3 b, Vector3 c)
    {   
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    public void TriBoundBox(ref int xMax, ref int xMin, ref int yMax, ref int yMin, List<Vector4> vertices)
    {
        //{ vertices[0].x, vertices[1].x, vertices[2].x }
        xMax = (int)Mathf.Max(Mathf.Max(vertices[0].x, vertices[1].x), vertices[2].x);
        xMin = (int)Mathf.Min(Mathf.Min(vertices[0].x, vertices[1].x), vertices[2].x);

        yMax = (int)Mathf.Max(Mathf.Max(vertices[0].y, vertices[1].y), vertices[2].y);
        yMin = (int)Mathf.Min(Mathf.Min(vertices[0].y, vertices[1].y), vertices[2].y);

        xMax = (int)Mathf.Min(xMax, Screen.width - 1);
        xMin = (int)Mathf.Max(xMin, 0);

        yMax = (int)Mathf.Min(yMax, Screen.height - 1);
        yMin = (int)Mathf.Max(yMin, 0);
    }

    public void DrawTriangles(List<Vector4> vertices, IShader _IShader,Vector3 hW) {
        
        //Per fragment variables
        float depth, uPers, vPers, areaPers, count = 0; //u, v, are perspective corrected
        Vector3 e, e_row, f;
        Vector4 rgbVals = new Vector4(1, 1, 1,1);
 
        float signOfTrig = Edge(vertices[0], vertices[1], vertices[2]);
        if (signOfTrig > 0) return;

        int xMax = 0;
        int xMin = 0;
        int yMax = 0;
        int yMin = 0;

        TriBoundBox(ref xMax, ref xMin, ref yMax, ref yMin, vertices);

        Vector3 zVals = new Vector3(vertices[0].z, vertices[1].z, vertices[2].z);
        float A01 = vertices[0].y - vertices[1].y, B01 = vertices[1].x - vertices[0].x;
        float A12 = vertices[1].y - vertices[2].y, B12 = vertices[2].x - vertices[1].x;
        float A20 = vertices[2].y - vertices[0].y, B20 = vertices[0].x - vertices[2].x;

        Vector3 point = new Vector3(xMin, yMin, 0);
         
        e_row.x = Edge(vertices[1], vertices[2], point);
        e_row.y = Edge(vertices[2], vertices[0], point);
        e_row.z = Edge(vertices[0], vertices[1], point);

        for (int y = yMin; y <= yMax; ++y)
        {
            //Bary coordinates at start of row
            e.x = e_row.x;
            e.y = e_row.y;
            e.z = e_row.z;
            for (int x = xMin; x <= xMax; ++x)
            {   
                //可以把下面注释的这些改成增量算法
                //Vector2 p = new Vector2(x, y);
                //float signOfAB = Edge(vertices[1], vertices[2], p);
                //float signOfBC = Edge(vertices[2], vertices[0], p);
                //float signOfCA = Edge(vertices[0], vertices[1], p);

                //Vector3 signPos = new Vector3(signOfAB, signOfBC, signOfCA);

                //if (signOfAB * signOfTrig > 0&& signOfBC * signOfTrig > 0&& signOfCA * signOfTrig > 0)
                if ((e.x <= 0) && (e.y <= 0) && (e.z <= 0))
                {
                    //Zbuffer check 
                    depth = Vector3.Dot((e * 1/signOfTrig), zVals);
                    //depth = Vector3.Dot((signPos * 1 /signOfTrig), zVals);

                    bool isPass = true;
                    if (RenderingMaster._instance.GetOpenZDepth()) {
                        float currentDepth = GetZbuffer(x, y);
                        if (currentDepth > depth && depth <= 1.0)
                        {
                            isPass = true;
                        }
                        else {
                            isPass = false;
                        }
                    }
                    if (isPass)
                    {
                        SetZbuffer(x, y, depth);

                        //Get perspective correct barycentric coords
                        //e* hW
                        f = new Vector3(e.x * hW.x, e.y * hW.y, e.z * hW.z);
                        areaPers = 1 / (f.x + f.y + f.z);
                        uPers = f.y * areaPers;
                        vPers = f.z * areaPers;

                        //Run fragment shader (U, v are barycentric coords)
                        rgbVals = _IShader.fragment(uPers, vPers);

                        DrawPoint(x, y, rgbVals);
                    }
                }

                e.x += A12;
                e.y += A20;
                e.z += A01;
            }

            e_row.x += B12;
            e_row.y += B20;
            e_row.z += B01;
        }
    }


}
