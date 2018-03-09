using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class WebCamPanel : MonoBehaviour
{
    public Text output;
    public RawImage image;
    public Button CaptureButton;
    private WebCamTexture _webCamTexture;

    private Tensor t;

    // Use this for initialization
    void Start()
    {
        StartCoroutine(SetupCoroutine());
        CaptureButton.onClick.AddListener(() => TakePhoto() );

        t = new Tensor(output);
    }

    public IEnumerator TakePhoto()
    {
        // NOTE - you almost certainly have to do this here:

        yield return new WaitForEndOfFrame();

        // it's a rare case where the Unity doco is pretty clear,
        // http://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html
        // be sure to scroll down to the SECOND long example on that doco page 

        Texture2D photo = new Texture2D(_webCamTexture.width, _webCamTexture.height);
        photo.SetPixels(_webCamTexture.GetPixels());
        photo.Apply();

        //Encode to a PNG
        Color32[] bytes = Rotate(photo.GetPixels32(), photo.width, photo.height);
        t.Parse(bytes);
        
        //Write out the PNG. Of course you have to substitute your_path for something sensible
        // File.WriteAllBytes("photo.png", bytes);
    }
    
    private Color32[] Rotate(Color32[] pixels, int width, int height)
    {
        return TextureTools.RotateImageMatrix(pixels, width, height, -90);
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
