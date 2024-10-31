using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimulationUIController : MonoBehaviour
{
    [SerializeField] private SimulationManager simulationManager;
    [Space]
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TMP_InputField spheresCountInput;
    [SerializeField] private Button addButton;
    [SerializeField] private TMP_Text currentSpheresCountText;


    private void Start()
    {
        startButton.onClick.AddListener(simulationManager.StartSimulation);
        stopButton.onClick.AddListener(simulationManager.StopSimulation);
        addButton.onClick.AddListener(AddSpheres);

        simulationManager.OnSpheresAdded += UpdateCurrentSpheresCount;
    }

    private void OnDestroy()
    {
        simulationManager.OnSpheresAdded -= UpdateCurrentSpheresCount;
    }

    private void AddSpheres()
    {
        var newSpheresCount = int.Parse(spheresCountInput.text);
        simulationManager.AddSpheres(newSpheresCount);
    }

    private void UpdateCurrentSpheresCount(int newCount)
    {
        currentSpheresCountText.text = simulationManager.SpheresCount.ToString();
    }
}