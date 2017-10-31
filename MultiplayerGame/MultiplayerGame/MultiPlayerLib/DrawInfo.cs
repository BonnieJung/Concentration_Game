using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;


namespace MultiPlayerLib
{
    public interface IDrawCallback
    {
        [OperationContract(IsOneWay = true)]
        void DrawLine(double x, double y,double prevx, double prevy);
    }


    [ServiceContract(CallbackContract = typeof(IDrawCallback))]
    public interface IDrawInfo 
    {
        DrawData dd { [OperationContract]get; [OperationContract(IsOneWay = true)]set; }

        [OperationContract(IsOneWay = true)]
        void updateAllUsers();
        string ToString();

        [OperationContract]
        bool Join(string name);
        [OperationContract(IsOneWay = true)]
        void Leave(string name);
    }

    [ServiceBehavior(InstanceContextMode =InstanceContextMode.Single)]
    public class DrawInfo: IDrawInfo
    {
        //public string userName { get; set; }
        public DrawData dd { get; set; }
        // public Dictionary<DrawData, IDrawCallback> ddAndCallback { get; set; }

        //data structure(proxy objects..)
        private Dictionary<string, IDrawCallback> drawCallbacks
            = new Dictionary<string, IDrawCallback>();
        


        /*-------------------IDrawInfo methods------------------------------*/
        public bool Join(string name)
        {
            if (drawCallbacks.ContainsKey(name.ToUpper())) //cf. shoe class...because it can be called multiple times... you only want to add it once
                // User alias must be unique
                return false;
            else
            {
                IDrawCallback cb = null;

                cb = OperationContext.Current.GetCallbackChannel<IDrawCallback>();

                // Save alias and callback proxy
                drawCallbacks.Add(name.ToUpper(), cb);

                Console.WriteLine(name.ToUpper() + " joined the service!");

                return true;
            }
        }

        public void Leave(string name)
        {
            if (drawCallbacks.ContainsKey(name.ToUpper()))
            {
                Console.WriteLine(name.ToUpper() + " left the service!");
                drawCallbacks.Remove(name.ToUpper());
            }
        }

        void IDrawInfo.updateAllUsers()
        {
            Console.WriteLine("Updating users.");
            foreach(IDrawCallback cb in drawCallbacks.Values)
            {
                cb.DrawLine(dd.X, dd.Y, dd.prevX,dd.prevY);
            }
        }


        public override string ToString()
        {
            
            return String.Format("X : {0} , Y : {1} \nPrev X : {2}, Prev Y : {3}\nColour : {4}\nThickness : {5}", dd.X, dd.Y, dd.prevX, dd.prevX, dd.prevY, dd.colour, dd.brushThickness);
        }
    }
}
