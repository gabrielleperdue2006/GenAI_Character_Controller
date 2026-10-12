using UnityEngine;

public class Walking : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    } // End of Start

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow) ==true)
        {
           
            if (transform.position.y < 10) {

            transform.Translate(0f, 0.01f, 0f);
            } // End of if statement (Player 1 moves up)
           
        } // end of if statement (Player 1 presses up arrow)

        if (Input.GetKey(KeyCode.DownArrow) == true)
        {
            if (transform.position.y > -10)
            {
                transform.Translate(0f, -0.01f, 0f);
            } // End of if statement (Player 1 moves down)
        } // end of if statement (Player 1 presses down arrow)

        if (Input.GetKey(KeyCode.LeftArrow) == true)
        {
            if (transform.position.x > -10)
            {
                transform.Translate(-0.01f, 0f, 0f);
            } // End of if statement (Player 1 moves left)
        } // end of if statement (Player 1 presses left arrow)

        if (Input.GetKey(KeyCode.RightArrow) == true)
        {
            if (transform.position.x < 10)
            {
                transform.Translate(0.01f, 0f, 0f);
            } // End of if statement (Player 1 moves right)
        } // end of if statement (Player 1 presses right arrow)


    } // End of Update
} // End of Walking
