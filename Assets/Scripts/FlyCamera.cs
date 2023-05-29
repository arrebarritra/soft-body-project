using UnityEngine;
using System.Collections;

public class FlyCamera : MonoBehaviour
{

    /*
    Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
    Converted to C# 27-02-13 - no credit wanted.
    Simple flycam I made, since I couldn't find any others made public.  
    Made simple to use (drag and drop, done) for regular keyboard layout  
    wasd : basic movement
    shift : Makes camera accelerate
    space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/


    public float mainSpeed = 10.0f; //regular speed
    float camSens = 0.25f; //How sensitive it with mouse
    private bool controlAngle = false;
    private Vector3 camAngle = new Vector3(0, 0, 0); //kind of in the middle of the screen, rather than at the top (play)
    private Vector3 lastMouse;
    private float totalRun = 1.0f;

    private void Start()
    {
        camAngle = transform.eulerAngles;
    }
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse1)) controlAngle = !controlAngle;
        lastMouse = Input.mousePosition - lastMouse;
        if (controlAngle)
        {
            camAngle = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
            camAngle = new Vector3(transform.eulerAngles.x + camAngle.x, transform.eulerAngles.y + camAngle.y, 0);
        }
        transform.eulerAngles = camAngle;
        lastMouse = Input.mousePosition;
        //Mouse  camera angle done.  

        //Keyboard commands
        Vector3 p = GetBaseInput();
        if (p.sqrMagnitude > 0)
        { // only move while a direction key is pressed
            totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
            p = p * mainSpeed;
            p = p * Time.deltaTime;
            Vector3 newPosition = transform.position;
            if (Input.GetKey(KeyCode.Space))
            { //If player wants to move on X and Z axis only
                transform.Translate(p);
                newPosition.x = transform.position.x;
                newPosition.z = transform.position.z;
                transform.position = newPosition;
            }
            else
            {
                transform.Translate(p);
            }
        }
    }

    private Vector3 GetBaseInput()
    { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}