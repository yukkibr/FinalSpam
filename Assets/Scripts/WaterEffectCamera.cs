using UnityEngine;

public class WaterEffectCamera : MonoBehaviour
{
    public GameObject waterEffectPanel;

    private int waterTriggerCount = 0;

    void Start()
    {
        if (waterEffectPanel != null)
        {
            waterEffectPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Water"))
            return;

        waterTriggerCount++;

        if (waterEffectPanel != null)
        {
            waterEffectPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Water"))
            return;

        waterTriggerCount = Mathf.Max(0, waterTriggerCount - 1);

        if (waterTriggerCount == 0 && waterEffectPanel != null)
        {
            waterEffectPanel.SetActive(false);
        }
    }
}
