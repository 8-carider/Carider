using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MapSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class MapData
    {
        public string mapName;
        public Sprite previewImage;
        public string sceneName; // "stage_Practice", "stage_1", "stage_2"
    }

    public Image mapPreviewImage;   // MapPreviewImage (UI Image)
    public TMP_Text mapNameText;        // MapNameText (UI Text)
    public MapData[] maps;          // Inspector에서 3개로 설정
    int currentIndex = 0;

    void Start()
    {
        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("MapSelectionManager: maps 배열이 비어있습니다.");
            return;
        }
        currentIndex = 0;
        ShowMap(currentIndex);
    }

    void ShowMap(int index)
    {
        if (maps == null || maps.Length == 0) return;
        index = Mathf.Clamp(index, 0, maps.Length - 1);
        mapPreviewImage.sprite = maps[index].previewImage;
        mapNameText.text = maps[index].mapName;
    }

    public void NextMap()
    {
        if (maps == null || maps.Length == 0) return;
        currentIndex = (currentIndex + 1) % maps.Length;
        ShowMap(currentIndex);
    }

    public void PrevMap()
    {
        if (maps == null || maps.Length == 0) return;
        currentIndex = (currentIndex - 1 + maps.Length) % maps.Length;
        ShowMap(currentIndex);
    }

    // 로컬(네트워크 미연결)일 때 씬 로드
    public void PlaySelectedMap()
    {
        if (maps == null || maps.Length == 0) return;
        string sceneToLoad = maps[currentIndex].sceneName;
        SceneManager.LoadScene(sceneToLoad);
    }
}
