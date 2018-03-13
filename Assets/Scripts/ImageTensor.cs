using UnityEngine;
using TensorFlow;
using UnityEngine.UI;

public class ImageTensor {

    private string[] labels = { "other", "daisy" };
    
    private TextAsset graphModel;
    private TFGraph graph;
    private TFSession session;

    // Use this for initialization
    public ImageTensor() {
#if UNITY_ANDROID
        TensorFlowSharp.Android.NativeBinding.Init();
#endif

        graphModel = Resources.Load("daisy_only/retrained") as TextAsset;

        graph = new TFGraph();
        graph.Import(graphModel.bytes, "");
        session = new TFSession(graph);
    }

    private TFTensor GenerateTensor(byte[] image)
    {
#if UNITY_ANDROID
        TFShape tshape = new TFShape(1, 128, 128, 3);
        return TFTensor.FromBuffer(tshape, image, 0, image.Length);
#endif
#if UNITY_EDITOR_WIN
        // TODO: This dosen't work
        return ImageUtil.CreateTensorFromImageFile(image);
#endif
    }

    /// <summary>
    /// Partially based off of: https://github.com/migueldeicaza/TensorFlowSharp/blob/master/Examples
    /// </summary>
    /// <param name="tensor"></param>
    /// <param name="image"></param>
    public string Parse(TFTensor tensor, byte[] image)
    {
        if (image != null)
        {
            var runner = session.GetRunner();

            if (graph == null)
                return "Graph is null";
            if (graph["input"] == null)
                return "Input is null";
            if (graph["final_result"] == null)
                return "Output is null";

            runner.AddInput(graph["input"][0], tensor);
            runner.Fetch(graph["final_result"][0]);

            var output = runner.Run();
            
            var result = output[0];
            var rshape = result.Shape;
            if (result.NumDims != 2 || rshape[0] != 1)
            {
                var shape = "";
                foreach (var d in rshape)
                {
                    shape += $"{d} ";
                }
                shape = shape.Trim();
                Debug.LogError($"Error: expected to produce a [1 N] shaped tensor where N is the number of labels, instead it produced one with shape [{shape}]");
            }

            // You can get the data in two ways, as a multi-dimensional array, or arrays of arrays, 
            // code can be nicer to read with one or the other, pick it based on how you want to process
            // it
            bool jagged = true;

            var bestIdx = 0;
            float p = 0, best = 0;

            if (jagged)
            {
                var probabilities = ((float[][])result.GetValue(jagged: true))[0];
                for (int i = 0; i < probabilities.Length; i++)
                {
                    if (probabilities[i] > best)
                    {
                        bestIdx = i;
                        best = probabilities[i];
                    }
                }

            }
            else
            {
                var val = (float[,])result.GetValue(jagged: false);

                // Result is [1,N], flatten array
                for (int i = 0; i < val.GetLength(1); i++)
                {
                    if (val[0, i] > best)
                    {
                        bestIdx = i;
                        best = val[0, i];
                    }
                }
            }

            return $"{(best * 100.0).ToString().Substring(0, 5)}% {labels[bestIdx]}\n";
        }
        else
            return "ERROR: Bytes null";
    }

}
