using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TensorFlow;
using UnityEngine.UI;

public class Tensor {

    private string[] Labels = { "tulips", "roses", "dandelion", "sunflowers", "daisy" };

    public Text output;

    private TextAsset graphModel;
    private TFGraph graph;
    private TFSession session;

    // Use this for initialization
    public Tensor(Text output) {
        this.output = output;

        TensorFlowSharp.Android.NativeBinding.Init();
        graphModel = Resources.Load("1.4/graph") as TextAsset;
    }

    public void Import()
    {
        output.text = "importing graph";

        graph = new TFGraph();
        graph.Import(graphModel.bytes);
        session = new TFSession(graph);

        if (session != null)
            output.text += "\nimported graph";
    }

    public void ExParse(byte[] bytes)
    {
        var runner = session.GetRunner();
        if (runner != null)
            output.text += "\ngot runner";

        runner.AddInput(graph["input_placeholder_name"][0], new float[] { 0, 1 });
        runner.Fetch(graph["output_placeholder_name"][0]);
        float[,] recurrent_tensor = runner.Run()[0].GetValue() as float[,];
    }
    
    public float[,] Parse(byte[] bytes)
    {
        if (bytes != null)
        {
            var tensor = new TFTensor(bytes);
            var runner = session.GetRunner();
            Debug.Log("runner is not null");
            
            runner.AddInput(graph["encoded_image_bytes"][0], tensor);
            runner.Fetch("probabilities");
            return runner.Run()[0].GetValue() as float[,];
        }
        else
            output.text = "ERROR: Bytes null";
        return new float[,] { };
    }
}
