using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WebCamPanel : MonoBehaviour
{
    public Text output;
    public RawImage image;
    public Button CaptureButton;
    private WebCamTexture _webCamTexture;

    private ImageTensor imageTensor;

    // Use this for initialization
    void Start()
    {
        StartCoroutine(SetupCoroutine());
        CaptureButton.onClick.AddListener(() => StartCoroutine(TakePhoto()) );

        imageTensor = new ImageTensor();
    }

    public IEnumerator TakePhoto()
    {
        yield return new WaitForEndOfFrame();
        
        Texture2D photo = new Texture2D(_webCamTexture.width, _webCamTexture.height);
        photo.SetPixels(_webCamTexture.GetPixels());
        photo.Apply();
        
        TextureTools.scale(photo, 128, 128);
        var tensor = ImageUtil.TransformInput(photo.GetPixels32());

        //Encode to a PNG
        byte[] bytes = photo.EncodeToJPG();
        string result = imageTensor.Parse(tensor, bytes);

        output.text = result;
    }
    
    private IEnumerator SetupCoroutine()
    {
        while (_webCamTexture == null)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length > 0)
            {
                _webCamTexture = new WebCamTexture(devices[0].name);
                _webCamTexture.Play();

                yield return new WaitUntil(() => _webCamTexture.width > 10);

                image.texture = _webCamTexture;
                image.material.mainTexture = _webCamTexture;
                image.color = Color.white;
                bool rotated = TransformImage();
                SetImageSize(rotated);
            }
            yield return 0;
        }
    }

    private void SetImageSize(bool rotated)
    {
        var canvas = gameObject.GetComponentInParent<Canvas>();
        var canvasRect = canvas.pixelRect;
        var rotTexSize = rotated ? new Vector2(_webCamTexture.height, _webCamTexture.width) : new Vector2(_webCamTexture.width, _webCamTexture.height);
        float ratio = 1f;
        if (rotTexSize.x < canvasRect.width && rotTexSize.y > canvasRect.height)
        {
            ratio = canvasRect.width / rotTexSize.x;
        }
        else if (rotTexSize.x > canvasRect.width && rotTexSize.y < canvasRect.height)
        {
            ratio = canvasRect.height / rotTexSize.y;
        }
        else if (rotTexSize.x < canvasRect.width && rotTexSize.y < canvasRect.height)
        {
            var widthRatio = canvasRect.width / rotTexSize.x;
            var heightRatio = canvasRect.height / rotTexSize.y;
            ratio = widthRatio < heightRatio ? heightRatio : widthRatio;
        }
        else if (rotTexSize.x > canvasRect.width && rotTexSize.y > canvasRect.height)
        {
            var widthRatio = canvasRect.width / rotTexSize.x;
            var heightRatio = canvasRect.height / rotTexSize.y;
            ratio = widthRatio < heightRatio ? heightRatio : widthRatio;
        }

        var rect = image.gameObject.GetComponent<RectTransform>();
        var texSize = new Vector2(_webCamTexture.width, _webCamTexture.height);
        rect.sizeDelta = texSize * ratio / canvas.scaleFactor;
    }

    private bool TransformImage()
    {
#if UNITY_IOS
		// iOS cam is mirrored
        image.gameObject.transform.localScale = new Vector3(-1, 1, 1);
#endif
        image.gameObject.transform.Rotate(0.0f, 0, -_webCamTexture.videoRotationAngle);
        return _webCamTexture.videoRotationAngle != 0;
    }

    private void OnDestroy()
    {
        _webCamTexture.Stop();
        Destroy(_webCamTexture);
    }
}
