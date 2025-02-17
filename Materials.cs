using System;
using UnityEngine;

namespace Colorblind_Holds
{
    public class Materials
    {
        private GameObject obj;
        private Renderer renderer;
        private Material normalMaterial;
        private Material highlightMaterial;

        public string tag { get; }

        public Materials(GameObject obj, string tag, Material normalMaterial, Material highlightMaterial)
        {
            this.obj = obj;
            this.tag = tag;
            this.normalMaterial = normalMaterial;
            this.highlightMaterial = highlightMaterial;
            this.renderer = obj.GetComponent<Renderer>();
        }

        public void Enable(Color color)
        {
            if (renderer == null)
            {
                renderer = obj.AddComponent<MeshRenderer>();
            }

            renderer.material = highlightMaterial;
            renderer.material.color = color;
        }

        public void Disable()
        {
            if (renderer == null)
            {
                return;
            }

            if (normalMaterial == null)
            {
                GameObject.DestroyImmediate(renderer);
                renderer = null;
            }
            else
            {
                renderer.material = normalMaterial;
            }
        }
    }
}
