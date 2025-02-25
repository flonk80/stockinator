using Stockinator.Common.DataFetching;
using Stockinator.Logic;

var client = new HttpClient();

client.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

var dataFetcher = new DataFetcher(client);

var result = await dataFetcher.FetchStockPeriodAsync("AAPL", DateTime.Today.AddDays(-3650), DateTime.Today);

var joe = new TensorJoe([result]);

var pridction = joe.PredictPrice(((DateTimeOffset)DateTime.UtcNow.AddDays(1)).ToUnixTimeSeconds(), "AAPL");

Console.WriteLine(pridction);







//using Tensorflow.Keras.Engine;
//using Tensorflow.NumPy;
//using static Tensorflow.Binding;
//using static Tensorflow.KerasApi;
//using Tensorflow.Keras;
//using Tensorflow;
//using Tensorflow.Keras.Layers;
//using Tensorflow.Keras.Optimizers;
//using Tensorflow.Keras.ArgsDefinition.Rnn;
//using Tensorflow.Keras.ArgsDefinition;

//tf.enable_eager_execution();

//var model = CreateLSTMModel();

//var (xTrain, yTrain) = GenerateData(1000);

//model.fit(xTrain, yTrain, batch_size: 16, epochs: 10);

//var testTimestamp = new NDArray(new float[,] { { 1700000000 } }); // Example future timestamp
//var prediction = model.predict(testTimestamp);

//Console.WriteLine($"Predicted value: {prediction.numpy()}");

//Console.ReadLine();

//static IModel CreateLSTMModel()
//{
//    //var inputs = keras.Input(shape: new Shape(1, 1)); // Single timestamp input (time step, features)
//    //var lstm = keras.layers.LSTM(50, return_sequences: false).Apply(inputs);
//    //var dense = keras.layers.Dense(1).Apply(lstm);
//    //var model = keras.Model(inputs, dense);

//    //model.compile(optimizer: keras.optimizers.Adam(), loss: keras.losses.MeanSquaredError());
//    //model.summary();

//    //return model;




//    // Define input layer explicitly
//    //var model = keras.Sequential(
//    //[
//    //    new LSTM(new LSTMArgs
//    //    {
//    //        Units = 50,
//    //        ReturnSequences = false,
//    //        InputShape = new[] { 1, 1, }
//    //    }),

//    //    //keras.layers.LSTM(units: 50, return_sequences: false, input),
//    //    //keras.layers.Dense(units: 1),
//    //    new Dense(new DenseArgs
//    //    {
//    //        Units = 1
//    //    })
//    //]);

//    var model = keras.Sequential();

//    model.Layers.AddRange(new List<ILayer>
//    {
//        new LSTM(new LSTMArgs
//        {
//            Units = 50,
//            ReturnSequences = false,
//            InputShape = new[] { 1, 1, }
//        }),

//        //keras.layers.LSTM(units: 50, return_sequences: false, input),
//        //keras.layers.Dense(units: 1),
//        new Dense(new DenseArgs
//        {
//            Units = 1
//        })
//    });


//    // Create and compile the model
//    model.compile(optimizer: keras.optimizers.Adam(), loss: keras.losses.MeanSquaredError());

//    return model;
//}

//static (NDArray, NDArray) GenerateData(int numSamples)
//{
//    var rand = new Random();
//    var xData = new float[numSamples, 1, 1];
//    var yData = new float[numSamples, 1];

//    for (int i = 0; i < numSamples; i++)
//    {
//        float timestamp = i + 1609459200; // Example Unix timestamp
//        float value = (float)Math.Sin(timestamp / 1e6) + (float)rand.NextDouble() * 0.1f; // Some function of timestamp

//        xData[i, 0, 0] = timestamp;
//        yData[i, 0] = value;
//    }

//    return (new NDArray(xData), new NDArray(yData));
//}