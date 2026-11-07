using UnityEngine;

namespace ARBlackjack.Interaction
{
    public static class ChipPivot
    {
        public static Vector3 LocalCenter(GameObject chip)
        {
            var mf = chip.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return Vector3.zero;
            Vector3 c = mf.sharedMesh.bounds.center;
            c.y = 0f;
            return chip.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
        }

        public static float LocalHeight(GameObject chip)
        {
            var mf = chip.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return 0f;
            Vector3 size = mf.sharedMesh.bounds.size;
            Vector3 worldUp = mf.transform.TransformVector(new Vector3(0f, size.y, 0f));
            return chip.transform.InverseTransformVector(worldUp).magnitude;
        }

        public static void Recenter(GameObject chip)
        {
            chip.transform.position -= chip.transform.TransformVector(LocalCenter(chip));
        }
    }
}
