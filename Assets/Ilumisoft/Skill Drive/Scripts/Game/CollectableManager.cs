using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    public class CollectableManager : MonoBehaviour
    {
        public UnityAction OnAllCollected;

        [SerializeField]
        private GameObject collectablePrefab; // 수집품 프리팹 참조

        [SerializeField]
        private Vector2 spawnAreaSize = new Vector2(10f, 10f); // 생성 영역 크기

        Collectable[] collectables;

        int amount = 0;
        int collected = 0;

        private void Awake()
        {
            collectables = FindObjectsByType<Collectable>(FindObjectsSortMode.None);

            if (collectables != null)
            {
                amount = collectables.Length;

                foreach (var collectable in collectables)
                {
                    collectable.OnCollect += OnCollect;
                }
            }
        }

        private void OnCollect()
        {
            collected++;

            // 새로운 수집품 생성
            SpawnNewCollectable();

            if (collected == amount)
            {
                OnAllCollected?.Invoke();
            }
        }

        private void SpawnNewCollectable()
        {
            if (collectablePrefab == null) return;

            // 랜덤 위치 계산 (현재 위치 기준으로 spawnAreaSize 범위 내)
            float randomX = Random.Range(-spawnAreaSize.x/2, spawnAreaSize.x/2);
            float randomZ = Random.Range(-spawnAreaSize.y/2, spawnAreaSize.y/2);

            Vector3 spawnPosition = transform.position + new Vector3(randomX, 0.5f, randomZ);

            // Ray를 아래로 발사하여 지면 확인
            RaycastHit hit;
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                spawnPosition.y = hit.point.y + 0.5f; // 지면 위 0.5 유닛에 위치
            }

            // 새로운 수집품 생성
            GameObject newCollectable = Instantiate(collectablePrefab, spawnPosition, Quaternion.identity);
            Collectable collectableComponent = newCollectable.GetComponent<Collectable>();
            
            if (collectableComponent != null)
            {
                collectableComponent.OnCollect += OnCollect;
                amount++; // 전체 수집품 개수 증가
            }
        }
    }
}