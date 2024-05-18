using TMPro;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private TMP_InputField minInputField;
    [SerializeField] private TMP_InputField maxInputField;

    private ContainerManager _containerManager;
    private void Start()
    {
        _containerManager = FindObjectOfType<ContainerManager>();
        if (_containerManager != null)
        {
            minInputField.text = _containerManager.minLeafSize.ToString();
            maxInputField.text = _containerManager.maxLeafSize.ToString();
        }
    }

    public void OnGenerateClick()
    {
        
        if(_containerManager!=null)
        {
            _containerManager.minLeafSize = int.Parse(minInputField.text);
            _containerManager.maxLeafSize = int.Parse(maxInputField.text);
            _containerManager.Generate();
        }
    }

    public void OnMinTextChanged(string text)
    {
        if (text.Length != 0 &&!char.IsDigit(text[^1]))
        {
            minInputField.text = text.Substring(0, text.Length - 1);
        } 
    }

    public void OnMaxTextChanged(string text)
    {
        if (text.Length != 0 &&!char.IsDigit(text[^1]))
        {
            maxInputField.text = text.Substring(0, text.Length - 1);
        }
    }
}
