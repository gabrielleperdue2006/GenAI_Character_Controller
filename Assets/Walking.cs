using UnityEngine;

public class Walking : MonoBehaviour
{
    public float speed = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    } // End of Start

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow) ==true)
        {
           
            if (transform.position.y < 8) {

            transform.Translate(speed, 0f, 0f);

            } // End of if statement (Player moves up)
           
        } // end of if statement (Player 1 presses up arrow)

        if (Input.GetKey(KeyCode.DownArrow) == true)
        {
            if (transform.position.y > -8)
            {
                transform.Translate(-speed, 0f, 0f);
            } // End of if statement (Player moves down)

        } // end of if statement (Player 1 presses down arrow)

        if (Input.GetKey(KeyCode.LeftArrow) == true)
        {
            if (transform.position.x > -8)
            {
                transform.Translate(0f, 0f, -speed);
            } // End of if statement (Player moves left)

        } // end of if statement (Player 1 presses left arrow)

        if (Input.GetKey(KeyCode.RightArrow) == true)
        {
            if (transform.position.x < 8)
            {
                transform.Translate(0f, 0f, speed);
            } // End of if statement (Player moves right)

        } // end of if statement (Player 1 presses right arrow)

    } // End of Update
} // End of Walking class
