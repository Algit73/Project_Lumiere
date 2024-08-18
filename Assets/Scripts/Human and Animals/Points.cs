using System.Collections.Generic;
using UnityEngine;

public class Points : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    private Dictionary<string, Transform> _occupationPoints;
    private List<Transform> _availablePoints;

    private void Awake()
    {
        _occupationPoints = new Dictionary<string, Transform>();
        _availablePoints = new List<Transform>();

       foreach (Transform point in points) 
            _availablePoints.Add(point);
    }

    public Transform GetPoint()
    {
        if (_availablePoints.Count > 0) return _availablePoints[Random.Range(0, _availablePoints.Count)];
        else return null;
    }
    public Transform SetFirstPoint(string name)
    {
        if (_availablePoints.Count == 0)
        {
            Debug.Log("Not Enough Points");
            return null;
        }

        Transform point = _availablePoints[Random.Range(0, _availablePoints.Count)];

        _occupationPoints.Add(name, point);
        _availablePoints.Remove(point);

        return point;
    }
    public void SetPoint(string name, Transform point)
    {
        _availablePoints.Add(_occupationPoints[name]);
        _occupationPoints[name] = point;
        _availablePoints.Remove(point);
    }
}