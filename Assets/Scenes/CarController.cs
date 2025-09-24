using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

[RequireComponent(typeof(NNet))]
public class CarController : MonoBehaviour
{
    private Vector3 startPosition, startRotation;
    private NNet network;

    private GAManager gaManager;

    [Range(-1f, 1f)]
    public float a, t;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
    public float overallFitness;

    [Header("Importance Characteristics")]
    public float distanceMultiplier = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;

    [Header("Network Architecture")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    private float aSensor, bSensor, cSensor;

    [Header("Performance")]
    public int generationCount = 0;
    public int solutionCount = 0;
    public int epoch = 200;

    private List<int> generationList = new List<int>();
    private List<int> solutionList = new List<int>();
    private string filePath;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        network = GetComponent<NNet>();
    }

    public void ResetWithNetwork(NNet net)
    {
        network = net;
        Reset();
    }



    public void Reset()
    {

        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Death();
    }

    private void FixedUpdate()
    {

        InputSensors();
        lastPosition = transform.position;


        (a, t) = network.RunNetwork(aSensor, bSensor, cSensor);


        MoveCar(a, t);

        timeSinceStart += Time.deltaTime;

        CalculateFitness();

    }

    private void Death()
    {
        GameObject.FindObjectOfType<GAManager>().Death(overallFitness, network);
    }

    private void CalculateFitness()
    {

        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled / timeSinceStart;

        overallFitness = (totalDistanceTravelled * distanceMultiplier) + (avgSpeed * avgSpeedMultiplier) + (((aSensor + bSensor + cSensor) / 3) * sensorMultiplier);

        if (timeSinceStart > 20 && overallFitness < 45)
        {
            Death();
        }

        if (overallFitness >= 1000)
        {
            generationCount = gaManager.getGenCount() + 1;
            solutionCount++;
            generationList.Add(generationCount);
            solutionList.Add(solutionCount);
            Death();
        }

    }

    private void InputSensors()
    {

        Vector3 a = (transform.forward + transform.right);
        Vector3 b = (transform.forward);
        Vector3 c = (transform.forward - transform.right);

        Ray r = new Ray(transform.position, a);
        RaycastHit hit;

        if (Physics.Raycast(r, out hit))
        {
            aSensor = hit.distance / 20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = b;

        if (Physics.Raycast(r, out hit))
        {
            bSensor = hit.distance / 20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = c;

        if (Physics.Raycast(r, out hit))
        {
            cSensor = hit.distance / 20;
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }
    }

    private Vector3 inp;
    public void MoveCar(float v, float h)
    {
        inp = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, v * 11.4f), 0.02f);
        inp = transform.TransformDirection(inp);
        transform.position += inp;

        transform.eulerAngles += new Vector3(0, (h * 90) * 0.02f, 0);
    }

    public void decreaseEpoch()
    {
        epoch--;
    }

    public void checkEpoch()
    {
        if (this.epoch == 0)
        {
            writeToFile();
            Time.timeScale = 0;
        }
    }

    private void writeToFile()
    {
        float ratio = 0; ;
        filePath = getPath();
        StreamWriter writer = new StreamWriter(filePath);
        writer.WriteLine("Solution Count, Generation Count, Ratio");

        for (int i = 0; i < Math.Max(solutionList.Count, generationList.Count); i++)
        {
            if (i < solutionList.Count)
            {
                writer.Write(solutionList[i]);
            }
            writer.Write(",");

            if (i < generationList.Count)
            {
                writer.Write(generationList[i]);
            }
            writer.Write(",");
            ratio = (float)solutionList[i] / (float)generationList[i];
            writer.Write(ratio);
            writer.Write(System.Environment.NewLine);
        }

        writer.Flush();
        writer.Close();
    }

    private string getPath()
    {
        return Application.dataPath + "sols.csv";
    }
    }