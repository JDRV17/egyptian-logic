using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class NameLabel : MonoBehaviour
{
    [SerializeField] private string format = "Querido/a Dr. {0}";

    private void OnEnable()
    {
        GetComponent<TMP_Text>().text = string.Format(format, SessionData.Instance.PlayerName);
    }
}