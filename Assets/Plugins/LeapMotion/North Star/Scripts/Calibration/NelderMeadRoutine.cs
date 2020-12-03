using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.AR;
using Leap.Unity.AR.Testing;
//Nelder-Mead Implementation-----------------------------------------------------------
public class NelderMeadRoutine {
  [NonSerialized]
  public float[] centroidCoordinate;
  [NonSerialized]
  public List<Vertex> simplexVertices;
  Action<float[]> preCostFunc;
  Func<float> postCostFunc;
  float alpha, beta, gamma, delta;
  MonoBehaviour behaviour;
  public bool steppingSolver = true;

  public bool isBottomRigel = false;
  public bool isLeft = true;
  public float standardDelay = 0.1f;

  //Initializes the solver with a starting coordinate, a simplex dimension, 
  //and coefficients (strengths) for the various transformations.
  public IEnumerator initializeNelderMeadRoutine (
    float[] initialVertex,
    Action<float[]> preCostFunction,
    Func<float> postCostFunction,
    MonoBehaviour behaviour,
    float initialSimplexSize = 1f,
    float reflectionCoefficient = 1f,
    float contractionCoefficient = 0.5f,
    float expansionCoefficient = 2f,
    float shrinkageCoefficient = 0.5f) {

    //Assign the cost function; this function takes in a coordinate in parameter space
    //And outputs the "cost" associated with this parameter; the solver will try to minimize the cost
    preCostFunc = preCostFunction;
    postCostFunc = postCostFunction;
    this.behaviour = behaviour;

    //These are the amounts the simplex moves at each step of the optimization process
    //The bigger these numbers, the more aggressive the solver will be
    alpha = reflectionCoefficient;
    beta = contractionCoefficient;
    gamma = expansionCoefficient;
    delta = shrinkageCoefficient;

    //Create Initial Simplex: Make the first vertex the initialVertex
    //And all subsequent vertices are translated "initialSimplexSize"
    //along just that dimension.  This is a "Right Angle" Simplex
    simplexVertices = new List<Vertex>(initialVertex.Length + 1);
    yield return behaviour.StartCoroutine(constructRightAngleSimplex(initialVertex, initialSimplexSize));
    centroidCoordinate = new float[simplexVertices[0].coordinates.Length];

    //Sort the list of vertices in our simplex by their costs (Smallest to Largest Cost)
    simplexVertices.Sort((x, y) => x.cost.CompareTo(y.cost));

    steppingSolver = false;

    if (Config.TryRead("CalibrationImageDelay", ref standardDelay)) {
      Debug.Log("Loaded CalibrationImageDelay from Config: " + standardDelay);
    } else {
      Config.Write("CalibrationImageDelay", standardDelay);
    }
  }

  public IEnumerator stepSolver() {
    steppingSolver = true;
    //First, calculate the centroid of all the vertices (except the largest cost vertex)
    //This point will lie on the center of the lowest-cost-face of the simplex
    centroidCoordinate = new float[simplexVertices[0].coordinates.Length];
    for (int i = 0; i < simplexVertices.Count - 1; i++) {
      centroidCoordinate = centroidCoordinate.Plus(simplexVertices[i].coordinates);
    }
    centroidCoordinate = centroidCoordinate.DivideBy(centroidCoordinate.Length);

    //Next, reflect the highest-cost vertex across this centroid and record it
    Vertex reflectedVertex = new Vertex(centroidCoordinate.Plus(centroidCoordinate.Minus(simplexVertices[simplexVertices.Count - 1].coordinates).Times(alpha)), preCostFunc, simplexVertices[simplexVertices.Count - 1].originalIndex);
    yield return behaviour.StartCoroutine(setVertexCost(reflectedVertex));

    //Now that we've computed the necessary data, we can decide how the solver should proceed

    //First, check if our reflected point's cost is between our best cost and our second worst cost
    if (simplexVertices[0].cost <= reflectedVertex.cost && reflectedVertex.cost < simplexVertices[simplexVertices.Count - 2].cost) {
      simplexVertices[simplexVertices.Count - 1] = reflectedVertex;

      //Else, check if our reflected point has a lower cost than our best point, and exaggerate it if it does
    } else if (reflectedVertex.cost < simplexVertices[0].cost) {
      Vertex expandedVertex = new Vertex(centroidCoordinate.Plus(reflectedVertex.coordinates.Minus(centroidCoordinate).Times(gamma)), preCostFunc, simplexVertices[simplexVertices.Count - 1].originalIndex);
      yield return behaviour.StartCoroutine(setVertexCost(expandedVertex));

      //Take the lower cost of the two possible vertices
      simplexVertices[simplexVertices.Count - 1] = (expandedVertex.cost < reflectedVertex.cost) ? expandedVertex : reflectedVertex;

      //Else, check if our reflected point has a greater cost than our second worst point; contract it if it does
    } else if (reflectedVertex.cost >= simplexVertices[simplexVertices.Count - 2].cost) {
      Vertex contractedVertex;

      //Check if our reflected point is our new second-worst point, or...
      if (simplexVertices[simplexVertices.Count - 2].cost <= reflectedVertex.cost && reflectedVertex.cost < simplexVertices[simplexVertices.Count - 1].cost) {
        contractedVertex = new Vertex(centroidCoordinate.Plus(reflectedVertex.coordinates.Minus(centroidCoordinate).Times(beta)), preCostFunc, simplexVertices[simplexVertices.Count - 1].originalIndex);
        yield return behaviour.StartCoroutine(setVertexCost(contractedVertex));

        //If the contracted vertex is better than our reflected vertex...
        if (contractedVertex.cost < reflectedVertex.cost) {
          simplexVertices[simplexVertices.Count - 1] = contractedVertex;
        } else {
          //SHRINK - SUPER EXPENSIVE; RECALCULATES ENTIRE SIMPLEX
          for (int i = 1; i < simplexVertices.Count; i++) {
            simplexVertices[i] = new Vertex(simplexVertices[0].coordinates.Plus(simplexVertices[i].coordinates.Minus(simplexVertices[0].coordinates).Times(delta)), preCostFunc, simplexVertices[i].originalIndex);
            yield return behaviour.StartCoroutine(setVertexCost(simplexVertices[i]));
          }
        }

        //if our reflected point is worse than the worst point
      } else if (reflectedVertex.cost >= simplexVertices[simplexVertices.Count - 1].cost) {
        contractedVertex = new Vertex(centroidCoordinate.Plus(simplexVertices[simplexVertices.Count - 1].coordinates.Minus(centroidCoordinate).Times(beta)), preCostFunc, simplexVertices[simplexVertices.Count - 1].originalIndex);
        yield return behaviour.StartCoroutine(setVertexCost(contractedVertex));

        //If the contracted vertex is better than our worst vertex...
        if (contractedVertex.cost < simplexVertices[simplexVertices.Count - 1].cost) {
          simplexVertices[simplexVertices.Count - 1] = contractedVertex;
        } else {
          //SHRINK - SUPER EXPENSIVE; RECALCULATES ENTIRE SIMPLEX
          for (int i = 1; i < simplexVertices.Count; i++) {
            simplexVertices[i] = new Vertex(simplexVertices[0].coordinates.Plus(simplexVertices[i].coordinates.Minus(simplexVertices[0].coordinates).Times(delta)), preCostFunc, simplexVertices[i].originalIndex);
            yield return behaviour.StartCoroutine(setVertexCost(simplexVertices[i]));
          }
        }
      }
    }

    //Last, sort the list of vertices in our simplex by their costs (Smallest to Largest Cost)
    //TODO: Replace with an insertion sort of simplexVertices[simplexVertices.Count - 1] (except during Shrink)
    simplexVertices.Sort((x, y) => x.cost.CompareTo(y.cost));
    steppingSolver = false;
  }

  //This creates a right-angle simplex around the "initial vertex" position
  //This can be called to "reinitialize" the solver while it is running to
  //fix: it getting stuck, pre-converged simplices, or degenerate simplices.
  IEnumerator constructRightAngleSimplex(float[] initialVertex, float initialSimplexSize) {
    simplexVertices.Clear();
    simplexVertices.Add(new Vertex(initialVertex, preCostFunc, 0));
    yield return behaviour.StartCoroutine(setVertexCost(simplexVertices[0]));
    for (int i = 0; i < initialVertex.Length; i++) {
      float[] vertexCoordinate = new float[initialVertex.Length];
      Array.Copy(initialVertex, vertexCoordinate, initialVertex.Length);
      vertexCoordinate[i] += initialSimplexSize;

      simplexVertices.Add(new Vertex(vertexCoordinate, preCostFunc, i + 1));
      yield return behaviour.StartCoroutine(setVertexCost(simplexVertices[simplexVertices.Count-1]));
    }

    //Sort the list of vertices in our simplex by their costs (Smallest to Largest Cost)
    simplexVertices.Sort((x, y) => x.cost.CompareTo(y.cost));
  }

  IEnumerator setVertexCost(Vertex vert) {
    isBottomRigel = false;
    preCostFunc(vert.coordinates);
    yield return new WaitForSeconds(0.1f);
    vert.cost = postCostFunc()*1.2f;
    if (isLeft) {
      //yield return new WaitForSeconds(0.1f);
      //rigelCalib.leapImageRetriever.UpdateDeviceIDTexture(rigelCalib.calibrationDevices[2].deviceID);
    }
    //Debug.Log("Top Image Cost: "+ vert.cost);

    isBottomRigel = true;
    preCostFunc(vert.coordinates);
    if (isLeft) {
      //yield return new WaitForSeconds(0.016f);
      //rigelCalib.leapImageRetriever.UpdateDeviceIDTexture(rigelCalib.calibrationDevices[1].deviceID);
    }
    yield return new WaitForSeconds(0.1f);
    vert.cost += postCostFunc();
    Debug.Log("Combined Image Cost: " + vert.cost);
  }

  //Simple Container Struct that stores coordinates and an associated cost
  public class Vertex {
    public float[] coordinates;
    public float cost;
    public int originalIndex; // This is for visualization only!

    public Vertex(float[] Coordinates, Action<float[]> preCostFunction, int index = 0) {
      coordinates = Coordinates;
      //preCostFunction(coordinates);
      originalIndex = index;
      cost = float.MaxValue;
    }
  }
}