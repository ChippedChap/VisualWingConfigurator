using UnityEngine;

namespace VisualWingConfigurator
{
    class WingStats
    {
        #region Settings
        public Vector3 rootMidChordOffset = Vector3.zero;
        public Vector3 tipMidChordOffset = Vector3.left;
        public float rootChordLength = 1f;
        public float tipChordLength = 1f;

        public float ZOffset
        {
            get { return rootMidChordOffset.z; }
            set
            {
                tipMidChordOffset.z = value;
                rootMidChordOffset.z = value; 
            }
        }
        #endregion

        #region Characteristics

        public float LeadSweep 
        {
            get
            {
                Vector3 leadingEdge = (tipMidChordOffset + Vector3.up * tipChordLength / 2) - (rootMidChordOffset + Vector3.up * rootChordLength / 2);
                return Vector3.SignedAngle(Vector3.left, leadingEdge, Vector3.forward);
            }
        }
        public float TrailSweep 
        {
            get
            {
                Vector3 trailingEdge = (tipMidChordOffset - Vector3.up * tipChordLength / 2) - (rootMidChordOffset - Vector3.up * rootChordLength / 2);
                return Vector3.SignedAngle(Vector3.left, trailingEdge, Vector3.forward);
            }
        }
        public float B_2
        {
            get
            {
                return Mathf.Abs(Vector3.Dot(tipMidChordOffset - rootMidChordOffset, Vector3.left));
            }
        }
        public float MAC 
        {
            get
            {
                return (tipChordLength + rootChordLength) / 2;
            }
        }
        public float TaperRatio 
        {
            get
            {
                return tipChordLength / rootChordLength;
            }
        }
        public float MidChordSweep 
        {
            get
            {
                return (LeadSweep + TrailSweep) / 2;
            }
        }

        public float Area
        {
            get
            {
                return B_2 * (tipChordLength + rootChordLength) * 0.5f;
            }
        }
        #endregion

        public WingStats(float scale)
        {
            rootMidChordOffset *= scale;
            tipMidChordOffset *= scale;
            rootChordLength *= scale;
            tipChordLength *= scale;
        }

        public WingStats()
        {

        }

        public void Reset(float scale)
        {
            rootMidChordOffset = Vector3.zero;
            tipMidChordOffset = Vector3.left * scale;
            rootChordLength = scale;
            tipChordLength = scale;
        }
    }
}
