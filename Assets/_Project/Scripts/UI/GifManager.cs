using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video; // VideoPlayer 사용을 위해 필수!

public class GifManager : MonoBehaviour
{
    [System.Serializable]
    public struct CoffeeVideoData
    {
        public string coffeeName;       // 커피 이름 (구분용)
        public VideoClip[] videoClips;  // 해당 커피에 재생할 동영상 클립들
    }

    [Header("UI & Video Components")]
    [SerializeField] private RawImage displayRawImage;   // 비디오 화면이 나올 RawImage
    [SerializeField] private VideoPlayer videoPlayer;    // 영상을 재생할 VideoPlayer Component
    [SerializeField] private GameObject prevButton;      // 이전 버튼
    [SerializeField] private GameObject nextButton;      // 다음 버튼

    [Header("Video Data per Coffee")]
    [SerializeField] private CoffeeVideoData[] coffeeVideos; // 각 커피별 비디오 클립 배열

    private int currentCoffeeIndex = -1;
    private int currentVideoIndex = 0;

    public void OpenGifViewer(int coffeeIndex)
    {
        if (coffeeIndex < 0 || coffeeIndex >= coffeeVideos.Length) return;

        transform.SetAsLastSibling(); // 최상단 레이어로 이동
        currentCoffeeIndex = coffeeIndex;
        currentVideoIndex = 0;

        gameObject.SetActive(true);
        PlayCurrentVideo();
    }

    // 영상 재생 처리
    private void PlayCurrentVideo()
    {
        if (currentCoffeeIndex < 0 || currentCoffeeIndex >= coffeeVideos.Length) return;

        VideoClip[] clips = coffeeVideos[currentCoffeeIndex].videoClips;

        if (clips != null && clips.Length > 0 && currentVideoIndex < clips.Length)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = clips[currentVideoIndex]; // 영상 할당
                videoPlayer.isLooping = true;                // 무한 반복 재생
                videoPlayer.Play();                          // 재생 시작
            }
        }

        if (prevButton != null) prevButton.SetActive(true);
        if (nextButton != null) nextButton.SetActive(true);
    }

    // 다음 영상 버튼 OnClick
    public void NextFrame()
    {
        if (currentCoffeeIndex < 0) return;

        VideoClip[] clips = coffeeVideos[currentCoffeeIndex].videoClips;
        if (clips != null && currentVideoIndex < clips.Length - 1)
        {
            currentVideoIndex++;
            PlayCurrentVideo();
        }
    }

    // 이전 영상 버튼 OnClick
    public void PrevFrame()
    {
        if (currentCoffeeIndex < 0) return;

        if (currentVideoIndex > 0)
        {
            currentVideoIndex--;
            PlayCurrentVideo();
        }
    }

    // X 닫기 버튼 OnClick
    public void CloseGifViewer()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        gameObject.SetActive(false);
        currentCoffeeIndex = -1;
        currentVideoIndex = 0;
    }
}