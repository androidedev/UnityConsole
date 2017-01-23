using UnityEngine;
using System.Collections;
using oIndieUnity;

public class oConsoleTest : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        oIndieUnity.oConsole.instance.WriteString("Hi world!!!!");
        oIndieUnity.oConsole.instance.WriteString("Hi world with horizontal scroll!!!  *****************************************************************************************************************************************************************************************************************************************");
    }

    int cnt = 0;
    void Update()
    {
        // Fixed lines
        oConsole.instance.SetString("time 1: ", Time.deltaTime);
        oConsole.instance.SetString("time 2: ", Time.deltaTime);
        oConsole.instance.SetString("time 3: ", Time.deltaTime);
        oConsole.instance.SetString("time 4: ", Time.deltaTime);

        // Unity Log capture
        if (cnt < 200) // put MaxUnityLines below 200 to test how works
        {
            Debug.Log("Unity Debug.Log " + cnt.ToString());
            Debug.LogWarning("Unity Debug.LogWarning " + cnt.ToString());
        }

        // Free lines
        if (cnt < 200) // put MaxFreeLines below 200 to test how works
        {
            oConsole.instance.WriteString("Freeline nº" + cnt.ToString());
        }
        cnt++;

        // uncomment this to test error captures
        //Debug.LogError("******ERORRRRRRRRRRRRRRRRRRRR");

        // uncomment this to test assert captures
        //Debug.Assert(1 == 2, "ASSERTTTTTTTTTTTTTTTTTTTTTTTTTT");
    }
}
