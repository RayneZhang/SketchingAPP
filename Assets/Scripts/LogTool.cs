﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;
using System.IO;
using System;
using UnityEngine.Networking;

public class LogTool : MonoBehaviour {

    public Grid grid;
    public PlayerControl player;

    List<LineRenderer> allLines;

    // string path = "Assets/Resources/log.txt";
    string uploadurl = "http://127.0.0.1:8000/uploadlog";
    string downloadurl = "http://127.0.0.1:8000/downloadlog";

    string gridType = "GridISO empty ";
    string historyList = "########### History List ############\n";
    string historyItem = "##HistoryItem## ";

    string header;
    string body;

    char[] delimiterChars = { ',', ' ', '"'};

    void Start()
    {
        body = historyList;
    }

    /// <summary>
    /// Upload file to server using Post request.
    /// </summary>
    /// <returns></returns>
    IEnumerator UploadFile(string content)
    {
        // List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        // formData.Add(new MultipartFormDataSection("field1 = foo & field2 = bar"));
        // formData.Add(new MultipartFormFileSection("my file data", "myfile.txt"));

        WWWForm data = new WWWForm();
        data.AddField("file", content);

        UnityWebRequest www = UnityWebRequest.Post(uploadurl, data);
        yield return www.Send();

        if (www.isError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("File upload complete!");
        }
    }

    /// <summary>
    /// Download log file from server using Get request.
    /// </summary>
    /// <returns></returns>
    IEnumerator DownloadFile()
    {
        UnityWebRequest www = UnityWebRequest.Get(downloadurl);

        yield return www.Send();

        if (www.isError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Debug.Log(www.downloadHandler.text);
            ParseDownloadedLog(www.downloadHandler.text);
        }
    }

    /// <summary>
    /// 1. Add to the "body" log when clicking load project.
    /// 2. Write log to the current log.
    /// </summary>
    /// <param name="log">Content of log file.</param>
    void ParseDownloadedLog(string log)
    {
        StringReader sreader = new StringReader(log);

        body = null;

        // StreamReader reader = new StreamReader(path);
        header = sreader.ReadLine();
        body = sreader.ReadToEnd();
        

        string[] result = header.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
        List<Vector3> allLinesofLog = new List<Vector3>();
        int LineNum = int.Parse(result[2]);
        for (int i = 0; i < LineNum; i++)
        {
            int x1 = int.Parse(result[4 * i + 3]);
            int y1 = int.Parse(result[4 * i + 1 + 3]);
            int x2 = int.Parse(result[4 * i + 2 + 3]);
            int y2 = int.Parse(result[4 * i + 3 + 3]);


            Vector3 startingpoint = grid.Grid2Point(new Vector2(x1, y1));
            Vector3 endingpoint = grid.Grid2Point(new Vector2(x2, y2));
            allLinesofLog.Add(startingpoint);
            allLinesofLog.Add(endingpoint);
        }
        player.LoadLinesFromLog(allLinesofLog);

        body += historyItem;
        body += "Type: ProjLoad, ";
        body += "Line: (0,0:0,0), ";
        body += "Time: " + DateTime.Now + "\n";

        sreader.Close();
    }

    /// <summary>
    /// Add to the "body" log when adding a line.
    /// </summary>
    /// <param name="p1">The first point of the line.</param>
    /// <param name="p2">The second point of the line.</param>
    public void LineAdd(Vector3 p1, Vector3 p2) {
        body += historyItem;
        body += "Type: LineAdd, ";

        Vector2 logPoint1 = grid.Point2Grid(p1);
        Vector2 logPoint2 = grid.Point2Grid(p2);

        body += "Line: (" + logPoint1.x + "," + logPoint1.y + ":" + logPoint2.x + "," + logPoint2.y + "), ";
        body += "Time: " + DateTime.Now + "\n";
    }

    /// <summary>
    /// Add to the "body" log when deleting a line.
    /// </summary>
    /// <param name="p1">The first point of the line</param>
    /// <param name="p2">The second point of the line</param>
    public void LineDel(Vector3 p1, Vector3 p2) {
        body += historyItem;
        body += "Type: LineDel, ";

        Vector2 logPoint1 = grid.Point2Grid(p1);
        Vector2 logPoint2 = grid.Point2Grid(p2);

        body += "Line: (" + logPoint1.x + "," + logPoint1.y + ":" + logPoint2.x + "," + logPoint2.y + "), ";
        body += "Time: " + DateTime.Now + "\n";
    }

    /// <summary>
    /// Add to the "body" log when clearing all lines.
    /// </summary>
    public void ClearAll() {
        body += historyItem;
        body += "Type: ClearAll, ";
        body += "Line: (0,0:0,0), ";
        body += "Time: " + DateTime.Now + "\n";
    }

    /// <summary>
    /// 1.Add to the "body" log when clicking save project.
    /// 2.Send Post request to server(Write log to the log file).
    /// </summary>
    public void ProjSave()
    {
        body += historyItem;
        body += "Type: ProjSave, ";
        body += "Line: (0,0:0,0), ";
        body += "Time: " + DateTime.Now + "\n";

        header = getHeader();

        // Write some text to the text.txt file, but we don't need it in webGL.
        // StreamWriter writer = new StreamWriter(path, false);
        // writer.WriteLine(header + body);
        // writer.Close();

        // Upload log file.
        StartCoroutine(UploadFile(header + body));

        // Re-import the file to update the reference in the editor.
        //AssetDatabase.ImportAsset(path);
        //TextAsset asset = (TextAsset)Resources.Load("text");

        //Debug.Log(asset.text);
    }
    

    /// <summary>
    /// 1. Send Get request to server(Load the log file).
    /// </summary>
    public void ProjLoad() {
        // Download log file.
        StartCoroutine(DownloadFile());

    }

    /// <summary>
    /// Get the "header" of log.
    /// </summary>
    /// <returns>header</returns>
    private string getHeader() {
        allLines = player.getAllLines();
        if (allLines.ToArray().Length == 0) {
            header = gridType + 0 + " \n"; 
        }
        else
        {
            header = gridType + allLines.ToArray().Length + " ";
            foreach(LineRenderer line in allLines)
            {
                header += "\"";
                Vector2 start = grid.Point2Grid(line.GetPosition(0));
                Vector2 end = grid.Point2Grid(line.GetPosition(1));
                header += start.x + ", " + start.y + ", " + end.x + ", " + end.y;
                header += "\" ";
            }
            header += "\n";
        }
        return header;
    }
}