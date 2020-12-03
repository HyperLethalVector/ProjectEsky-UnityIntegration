using UnityEngine;

public class DisableMe : MonoBehaviour {
    public KeyCode disableEnableKey;
	void Update () {
        if (Input.GetKeyDown(disableEnableKey)){
            foreach (Transform child in transform) {
                child.gameObject.SetActive(!child.gameObject.activeSelf);
            }
        }
    }
}
