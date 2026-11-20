using UnityEngine;

namespace NTGH.Demo.OutfitSystem
{
    public class SimpleBoneIdle : MonoBehaviour
    {
        [SerializeField] private Transform m_spineBone;
        [SerializeField] private Transform m_headBone;
        [SerializeField] private Transform m_leftArmBone;
        [SerializeField] private Transform m_rightArmBone;

        [Header("Idle Motion")]
        [SerializeField] private float m_breathSpeed = 1.65f;
        [SerializeField] private float m_spineBobAmplitude = 0.014f;
        [SerializeField] private float m_headTiltDegrees = 0.8f;
        [SerializeField] private float m_armSwingDegrees = 0.65f;

        private Vector3 m_spineBasePos;
        private float m_headBaseZ;
        private float m_leftArmBaseZ;
        private float m_rightArmBaseZ;
        private bool m_initialized;

        private void Start()
        {
            CaptureBasePose();
        }

        private void CaptureBasePose()
        {
            if (m_spineBone != null)
            {
                m_spineBasePos = m_spineBone.localPosition;
            }

            if (m_headBone != null)
            {
                m_headBaseZ = m_headBone.localEulerAngles.z;
            }

            if (m_leftArmBone != null)
            {
                m_leftArmBaseZ = m_leftArmBone.localEulerAngles.z;
            }

            if (m_rightArmBone != null)
            {
                m_rightArmBaseZ = m_rightArmBone.localEulerAngles.z;
            }

            m_initialized = true;
        }

        private void Update()
        {
            if (!m_initialized)
            {
                CaptureBasePose();
            }

            float t = Time.time * m_breathSpeed;
            float wave = Mathf.Sin(t);
            float counterWave = Mathf.Sin(t + 0.6f);

            if (m_spineBone != null)
            {
                m_spineBone.localPosition = m_spineBasePos + new Vector3(0f, wave * m_spineBobAmplitude, 0f);
            }

            if (m_headBone != null)
            {
                m_headBone.localRotation = Quaternion.Euler(0f, 0f, m_headBaseZ + counterWave * m_headTiltDegrees);
            }

            if (m_leftArmBone != null)
            {
                m_leftArmBone.localRotation = Quaternion.Euler(0f, 0f, m_leftArmBaseZ + wave * m_armSwingDegrees);
            }

            if (m_rightArmBone != null)
            {
                m_rightArmBone.localRotation = Quaternion.Euler(0f, 0f, m_rightArmBaseZ - wave * m_armSwingDegrees);
            }
        }

        public void SetupBones(Transform spine, Transform head, Transform leftArm, Transform rightArm)
        {
            m_spineBone = spine;
            m_headBone = head;
            m_leftArmBone = leftArm;
            m_rightArmBone = rightArm;
        }
    }
}
