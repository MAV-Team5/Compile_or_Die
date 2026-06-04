using UnityEngine;
using System.Collections.Generic;

public class GridRender : MonoBehaviour
{
    public Camera cam;
    public GameObject linePrefab;

    float cellSize = 1f;
    int step = 8;

    int range = 64; // 화면 밖 여유 범위

    private List<LineRenderer> lines = new List<LineRenderer>();

    void Start()
    {
        int maxLines = range * 4;

        for (int i = 0; i < maxLines; i++)
        {
            GameObject obj = Instantiate(linePrefab, transform);
            obj.transform.position = Vector3.zero;
            lines.Add(obj.GetComponent<LineRenderer>());
        }
    }

    void LateUpdate()
    {
        Vector3 camPos = cam.transform.position;

        // 카메라 스냅
        camPos.x = Mathf.Round(camPos.x * step) / step;
        camPos.y = Mathf.Round(camPos.y * step) / step;

        float startX = Mathf.Floor((camPos.x - range) / step) * step;
        float endX   = Mathf.Floor((camPos.x + range) / step) * step;

        float startY = Mathf.Floor((camPos.y - range) / step) * step;
        float endY   = Mathf.Floor((camPos.y + range) / step) * step;

        int index = 0;

        // 세로선
        for (float x = startX; x <= endX; x += step)
        {
            if (index >= lines.Count) return;

            var line = lines[index++];
            line.positionCount = 2;

            line.SetPosition(0, new Vector3(x, startY, -1f));
            line.SetPosition(1, new Vector3(x, endY, -1f));
        }

        // 가로선
        for (float y = startY; y <= endY; y += step)
        {
            if (index >= lines.Count) return;

            var line = lines[index++];
            line.positionCount = 2;

            line.SetPosition(0, new Vector3(startX, y, -1f));
            line.SetPosition(1, new Vector3(endX, y, -1f));
        }
    }
}