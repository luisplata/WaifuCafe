using StateMachines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V2.Staff;

public class StaffUiComponent : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI staffStatus;
    [SerializeField] private StaffStateMachine staffStateMachine;
    [SerializeField] private Image staffSlider;

    private void Start()
    {
        staffStateMachine.OnStateChange += UpdateStatusUi;
        staffStateMachine.OnLoadingState += UpdateStatusSlider;
    }

    private void UpdateStatusSlider(float obj)
    {
        staffSlider.fillAmount = obj;
    }

    private void UpdateStatusUi(StaffPhase obj)
    {
        staffStatus.text = $"Staff status: {obj}";
    }

    private void OnDestroy()
    {
        staffStateMachine.OnStateChange -= UpdateStatusUi;
        staffStateMachine.OnLoadingState -= UpdateStatusSlider;
    }
}