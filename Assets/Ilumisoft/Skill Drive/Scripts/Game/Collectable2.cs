using UnityEngine;
using Photon.Pun;

public class Collectable2 : MonoBehaviourPun 
{
    [Header("Settings")]
    [SerializeField] public int scoreAmount = 5; // 점수 설정

    [Header("Effects")]
    [SerializeField]
    GameObject particlesPrefab = null; // 파티클 프리팹 변수 (인스펙터 연결용)

    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf) return;

        if (other.CompareTag("Player"))
        {
            #if PHOTON_UNITY_NETWORKING
            if (particlesPrefab != null)
            {
                if (PhotonNetwork.InRoom)
                {
                    // 파티클 프리팹은 Resources 폴더에 있어야 하며, 이름이 정확히 일치해야 함
                    PhotonNetwork.Instantiate("Collect Particles 2", transform.position, transform.rotation);
                }
                else
                {
                    Instantiate(particlesPrefab, transform.position, transform.rotation);
                }
            }
            #else
            if (particlesPrefab != null)
            {
                Instantiate(particlesPrefab, transform.position, transform.rotation);
            }
            #endif

            var vehicle = other.GetComponentInParent<Ilumisoft.SkillDrive.Game.VehicleCollectables>();
            if (vehicle != null)
            {
                vehicle.AddCollectable(this.scoreAmount);
            }

            if (photonView != null)
            {
                photonView.RPC("DeactivateCollectableRPC", RpcTarget.OthersBuffered);
            }
            
            gameObject.SetActive(false);
        }
    }

    [PunRPC]
    private void DeactivateCollectableRPC()
    {
        if (particlesPrefab != null)
        {
            #if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.Instantiate("Collect Particles 2", transform.position, transform.rotation);
            }
            else
            {
                Instantiate(particlesPrefab, transform.position, transform.rotation);
            }
            #else
            Instantiate(particlesPrefab, transform.position, transform.rotation);
            #endif
        }
        gameObject.SetActive(false);
    }
}