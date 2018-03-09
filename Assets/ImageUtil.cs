using System;
using System.IO;
using TensorFlow;
using UnityEngine;

public static class ImageUtil
{
    public static TFTensor TransformInput(Color32[] pic)
    {
        const int INPUT_SIZE = 128;
        const int IMAGE_MEAN = 128;
        const float IMAGE_STD = 128;

        float[] floatValues = new float[(INPUT_SIZE * INPUT_SIZE) * 3];
        
        for (int i = 0; i < pic.Length; i++)
        {
            var color = pic[i];

            floatValues[i * 3] = (color.r - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
        }
        
        TFShape shape = new TFShape(1, INPUT_SIZE, INPUT_SIZE, 3);
        return TFTensor.FromBuffer(shape, floatValues, 0, floatValues.Length);
    }

    // Convert the image in filename to a Tensor suitable as input to the Inception model.
    public static TFTensor CreateTensorFromImageFile(byte[] file, TFDataType destinationDataType = TFDataType.Float)
    {
        // DecodeJpeg uses a scalar String-valued tensor as input.
        var tensor = TFTensor.CreateString(file);

        TFOutput input, output;

        // Construct a graph to normalize the image
        using (var graph = MobileConstructGraphToNormalizeImage(out input, out output, destinationDataType))
        {
            // Execute that graph to normalize this one image
            using (var session = new TFSession(graph))
            {
                var normalized = session.Run(
                    inputs: new[] { input },
                    inputValues: new[] { tensor },
                    outputs: new[] { output });

                return normalized[0];
            }
        }
    }

    private static TFGraph MobileConstructGraphToNormalizeImage(out TFOutput input, out TFOutput output, TFDataType destinationDataType = TFDataType.Float)
    {
        // Some constants specific to the pre-trained model at:
        // https://storage.googleapis.com/download.tensorflow.org/models/inception5h.zip
        //
        // - The model was trained after with images scaled to 224x224 pixels.
        // - The colors, represented as R, G, B in 1-byte each were converted to
        //   float using (value - Mean)/Scale.

        const int W = 128;
        const int H = 128;
        const float Mean = 128;
        const float Scale = 1;

        var graph = new TFGraph();
        input = graph.Placeholder(TFDataType.String);

        output = graph.Cast(graph.Div(
            x: graph.Sub(
                x: graph.ResizeBilinear(
                    images: graph.ExpandDims(
                        input: graph.Cast(
                            graph.DecodeJpeg(contents: input, channels: 3), DstT: TFDataType.Float),
                        dim: graph.Const(0, "make_batch")),
                    size: graph.Const(new int[] { W, H }, "size")),
                y: graph.Const(Mean, "mean")),
            y: graph.Const(Scale, "scale")), destinationDataType);

        return graph;
    }
}
