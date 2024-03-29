using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LineGenerator : MonoBehaviour
{
    public static LineGenerator instance;

    public GameObject linePrefab;
    public GameObject Slider;
    public Canvas parentCanvas;
    public string selectedColor = "black";

    private UnityEngine.Color color;
    private Material material;
    public Material black, red, green, blue, orange;


    List<GameObject> Lines = new List<GameObject>();
    LineRenderer lineRenderer;
    Line activeLine;

    public float width = 1f;
    //[SerializeField]
    //float tolerance = .1f;   |  Maybe introduce to make it fancier


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }


    private void Start()
    {
        Material material = new Material(Shader.Find("Unlit/Color"));
        material.color = Color.black;
        this.material = material;
        EventSystem.instance.PressColorButton += OnPressColorButton;
        EventSystem.instance.DeleteAllLines += OnDeleeteAllLines;
        EventSystem.instance.DeleteLastLine += OnDeleteLastLine;
        EventSystem.instance.ShowLines += ShowLines;
        EventSystem.instance.HideLines += HideLines;
    }

    
    private void OnDeleeteAllLines()
    {
        foreach (GameObject line in Lines)
        {
            Destroy(line);
        }
        Lines.Clear();
    }

    private void OnDeleteLastLine()
    {
        if (Lines.Count > 0)
        {
            Destroy(Lines[Lines.Count - 1]);
            Lines.RemoveAt(Lines.Count - 1);
        }
    }

    private void OnPressColorButton(UnityEngine.Color color)
    {
        selectedColor = color.ToString();
        this.color = color;
        material.color = color;
    }
 

    private void OnDestroy()
    {
        foreach (GameObject line in Lines)
        {
            Destroy(line);
        }
    }


    public void HideLines()
    {
        foreach (GameObject line in Lines)
        {
            line.SetActive(false);
        }
    }

    public void ShowLines()
    {
        foreach (GameObject line in Lines)
        {
            line.SetActive(true);
        }
    }

    public int CountStrokes()
    {
        return Lines.Count;
    }

    public int CountPoints()
    {
        int count = 0;
        foreach (GameObject line in Lines)
        {
            count += line.GetComponent<Line>().lineRenderer.positionCount;
        }
        return count;
    }

    // Drawing functions
    bool drawOnImage()
    {
        GraphicRaycaster gr = parentCanvas.GetComponent<GraphicRaycaster>();
        PointerEventData ped = new PointerEventData(null);
        ped.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        gr.Raycast(ped, results);



        if (results.Count == 1 && results[0].gameObject.name == "DrawingBackground")
        {
            return true;
        }
       
        else
        {
            return false;
        }
    }

    Vector3 GetMousePosition()
    {
        Vector2 movePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition, parentCanvas.worldCamera,
            out movePos);
        Vector3 positionToReturn = parentCanvas.transform.TransformPoint(movePos);
        positionToReturn.z = parentCanvas.transform.position.z - 0.01f;
        return positionToReturn;
    }


    // Update is called once per frame
    void Update()
    {
        //width = Slider.GetComponent<Slider>().value;
        {
            
            if (!drawOnImage())
            {
                activeLine = null;
            } else 
            {

           
            if (Input.GetMouseButtonDown(0) )
            {
                
                    GameObject newLine = Instantiate(linePrefab, parentCanvas.transform);
                    Lines.Add(newLine);
                    lineRenderer = newLine.GetComponent<LineRenderer>();
                    lineRenderer.material = material;
                    lineRenderer.material.renderQueue = 2000 + Lines.Count;
                    lineRenderer.startWidth = width;
                    activeLine = newLine.GetComponent<Line>();
                            
            }

            if (Input.GetMouseButtonUp(0))
            {
                //lineRenderer.Simplify(tolerance);
                /* (lineRenderer.positionCount == 1)
                {
                    Vector3 mousePos = GetMousePosition();
                    activeLine.UpdateLine(mousePos);
                }*/
                activeLine = null;
            }

            if (activeLine != null)
            {
                
                    Vector3 mousePos = GetMousePosition();
                    activeLine.UpdateLine(mousePos);
                
               
            
            }
        } }
    }
}