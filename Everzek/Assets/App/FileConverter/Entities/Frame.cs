using UnityEngine;
namespace OpenEQ.FileConverter.Entities
{

    public class Frame
    {
        public string _name;
        public Vector3 position;
        public Quaternion rotation;

        public Frame(string name, float shiftx, float shifty, float shiftz, float rotx, float roty, float rotz, float rotw)
        {
            _name = name;
            position = new Vector3(shiftx, shifty, shiftz);
            rotation = new Quaternion(rotx, roty, rotz, rotw);
        }
    }
}