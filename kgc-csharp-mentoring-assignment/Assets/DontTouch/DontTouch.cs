using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontTouch : MonoBehaviour
{
    private static int dividee, divider;
    private static bool result = false;

    private TextMesh myText;
    private Collider myCollider;
    private Animator myAnimator;

    private TextMesh outText;

    private void Start()
    {
        myText = GetComponentInChildren<TextMesh>();
        myCollider = GetComponentInChildren<Collider>();
        myAnimator = GetComponent<Animator>();

        outText = GameObject.Find("Out").GetComponentInChildren<TextMesh>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit);

            if (hit.collider != myCollider)
            {
                return;
            }

            myAnimator.SetTrigger("Press");

            if (result)
            {
                outText.text = "0";
                result = false;
            }

            switch (myText.text)
            {
                case "=":
                    divider = int.Parse(outText.text);
                    outText.text = Assignment1.Divide(dividee, divider).ToString();
                    result = true;
                    return;

                case "÷":
                    dividee = int.Parse(outText.text);
                    outText.text = "0";
                    return;
            }

            if (outText.text == "0")
            {
                outText.text = "";
            }

            outText.text += myText.text;
        }
    }
}
