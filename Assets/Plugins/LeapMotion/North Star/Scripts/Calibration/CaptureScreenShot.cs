using UnityEngine;
public class CaptureScreenShot : MonoBehaviour {
  public int resWidth = 2560 ;
  public int resHeight = 1440;

  private bool takeHiResShot = false;

  private Camera cam;
  private void Start() {
    cam = GetComponent<Camera>();
  }

  public string ScreenShotName(int width, int height) {
    return string.Format("{0}/{1}_screen_{2}x{3}_{4}.png",
                         Application.dataPath,
                         transform.parent.gameObject.name,
                         width, height,
                         System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
  }

  public void TakeHiResShot() {
    takeHiResShot = true;
  }

  void LateUpdate() {
    takeHiResShot |= Input.GetKeyDown("k");
    if (takeHiResShot) {
      RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
      cam.targetTexture = rt;
      Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
      cam.Render();
      RenderTexture.active = rt;
      screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
      cam.targetTexture = null;
      RenderTexture.active = null; // JC: added to avoid errors
      Destroy(rt);
      byte[] bytes = screenShot.EncodeToPNG();
      string filename = ScreenShotName(resWidth, resHeight);
      System.IO.File.WriteAllBytes(filename, bytes);
      Debug.Log(string.Format("Took screenshot to: {0}", filename));
      takeHiResShot = false;
    }
  }
}