using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialLogue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private string[] lines;
    [SerializeField] private float textDelay;

    [SerializeField] private GameObject moveFragment;

    private int index;
    private int Segmentindex;
    // Start is called before the first frame update
    void Start()
    {
        textComponent.text = string.Empty;
        StartDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && gameObject.GetComponent<Image>().enabled == true)
        {
            if (textComponent.text == lines[index])
            {
                NextLine();
                
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
        }
    }

    void StartDialogue()
    {
        index = 0;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray()) 
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textDelay * 0.1f);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            gameObject.GetComponent<Image>().enabled = false;

            if (textComponent.text == "But I cant even move, How am I supposed to collect the fragments!")
                moveFragment.SetActive(true);

            textComponent.text = string.Empty;
        }
    }

    public void AddDialogue(string[] newLines)
    {
        index = 0;
        textComponent.text = string.Empty;
        
        lines = newLines;

        gameObject.GetComponent<Image>().enabled = true;

        StartDialogue();
    }
}
