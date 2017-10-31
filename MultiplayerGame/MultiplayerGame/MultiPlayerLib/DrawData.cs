using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace MultiPlayerLib
{
    [DataContract]
    public class DrawData
    {
        //if x,y and prevx,prevy are the same it is a point
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double prevX { get; set; }
        [DataMember]
        public double prevY { get; set; }
        [DataMember]
        public string colour { get; set; }
        [DataMember]
        public int brushThickness { get; set; }

        public DrawData(double x, double y, double px, double py, string c, int th)
        {
            X = x;
            Y = y;
            prevX = px;
            prevY = py;
            colour = c;
            brushThickness = th;
        }
        

        public override string ToString()
        {
            return String.Format("X : {0} , Y : {1} \nPrev X : {2}, Prev Y : {3}\nColour : {4}\nThickness : {5}", X, Y, prevX, prevX, prevY, colour, brushThickness);
        }
    }
}
