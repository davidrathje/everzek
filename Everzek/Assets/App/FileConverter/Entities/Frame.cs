
namespace OpenEQ.FileConverter.Entities
{
    using UnityEngine;

    public class Frame
    {
        public string _name;
        public Vector3 position;
        public Vector4 rotation;

        public Frame(string name, float shiftx, float shifty, float shiftz, float rotx, float roty, float rotz, float rotw)
        {
            _name = name;
            position = new Vector3(shiftx, shifty, shiftz);
            rotation = new Vector4(rotx, roty, rotz, rotw);
        }
    }
}