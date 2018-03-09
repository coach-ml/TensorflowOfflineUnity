using System.IO;
using TensorFlow;
using UnityEngine;

public static class ImageUtil
{
    private static int INPUT_SIZE = 128;
    private static int IMAGE_MEAN = 128;
    private static float IMAGE_STD = 1;

    public static TFTensor TransformInput(Color32[] pic)
    {
        float[] floatValues = new float[INPUT_SIZE * INPUT_SIZE * 3];

        for (int i = 0; i < pic.Length; ++i)
        {
            var color = pic[i];

            floatValues[i * 3 + 0] = (color.r - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
        }

        TFShape shape = new TFShape(1, INPUT_SIZE, INPUT_SIZE, 3);

        return TFTensor.FromBuffer(shape, floatValues, 0, floatValues.Length);
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
