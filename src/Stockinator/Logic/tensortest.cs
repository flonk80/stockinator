using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;
using Tensorflow.Keras;
using Tensorflow;
using Tensorflow.Keras.Layers;
using Tensorflow.Keras.Optimizers;
using Tensorflow.Keras.ArgsDefinition.Rnn;
using Tensorflow.Keras.ArgsDefinition;

namespace Stockinator.Logic
{
    public class tensortest
    {
        NDArray x_train, y_train, x_test, y_test;

        public void PrepareData()
        {
            (x_train, y_train) = GenerateData(1000);
            //(x_train, y_train, x_test, y_test) = keras.datasets.mnist.load_data();
            //x_train = x_train.reshape((60000, 784)) / 255f;
            //x_test = x_test.reshape((10000, 784)) / 255f;
        }

        public IModel BuildModel()
        {
            var inputs = keras.Input(shape: (1, 1));
            var layers = new LayersApi();

            var outputs = layers.Dense(50, activation: keras.activations.Relu).Apply(inputs);

            outputs = layers.Dense(25).Apply(outputs);
            outputs = layers.Dense(1).Apply(outputs);

            var model = keras.Model(inputs, outputs, name: "mnist_model");
            model.summary();

            model.compile(optimizer: keras.optimizers.Adam(), loss: keras.losses.MeanSquaredError(), metrics: new string[] { "mae" });

            return model;
        }

        public void Train()
        {
            //model.fit(x_train, y_train, batch_size: 10, epochs: 2);
            var model = BuildModel();

            model.fit(x_train, y_train, batch_size: 10, epochs: 10);

            var testTimestamp = new NDArray(new float[,] { { 1700000000 } }); // Example future timestamp
            var prediction = model.predict(testTimestamp);

            Console.WriteLine($"Predicted value: {prediction.numpy()}");

            Console.ReadLine();

            //model.evaluate(x_test, y_test);
        }

        private static (NDArray, NDArray) GenerateData(int numSamples)
        {
            var rand = new Random();
            var xData = new float[numSamples, 1, 1];
            var yData = new float[numSamples, 1];

            for (int i = 0; i < numSamples; i++)
            {
                float timestamp = i + 1609459200; // Example Unix timestamp
                float value = (float)Math.Sin(timestamp / 1e6) + (float)rand.NextDouble() * 0.1f; // Some function of timestamp

                xData[i, 0, 0] = timestamp;
                yData[i, 0] = value;
            }

            return (new NDArray(xData), new NDArray(yData));
        }
    }
}
