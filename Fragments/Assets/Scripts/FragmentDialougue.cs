using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentDialougue : MonoBehaviour
{

    [SerializeField] private string[] onMoveLines;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void moveDialogue()
    {
        GameObject.Find("DialogueBox").GetComponent<DialLogue>().AddDialogue(onMoveLines);
    }
}
