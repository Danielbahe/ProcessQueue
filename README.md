# ProcessQueue
An asynchronous .Net Queue to process anything on background. 

# Compatibility
- .NetStamdard 2.0, in other words, a lot of compatibility!
- Xamarin (Projects that accept .NetStandard 2.0) PCL version coming soon.

# How to use it
(Samples on project)
- First you have to create an instance of QueueManager
- After that you can setup your queue with the following options:
  - Notify errors (Totally customizable)
  - Stop queue on error
  - Save a backup of the queue locally (completly up to you how to do it, awesome!)
  - Enable priority
- Add your process (After the example how to create one)
- Run!

 ```
var queueManager = new QueueManager();
queueManager.StopWorkerOnError()
      .NotifyOnError(s => PublishErrorEvent()) //your custom action to notify the way you want it
      .GenerateBackUpQueue(SaveJsonOnDb, GetJsonFromDb) // custom actions to save the queue on db for example
      .EnablePriority()
      .AddProcess(new ConsoleWriteProcessable()) // Simple process
      .AddProcess(new ConsoleWriteProcessable(),1) //Process with priority
      .AddProcess(new ConsoleWriteProcessable(), 1, "myId") // Process with priority and Id
      .AddProcess(new List<IProcessable>{new ConsoleWriteProcessable()}) // List of Processables on a process
      .Start();
 ```
 - Now you have to create your own process with your code and stuff.
 You have to create a class that implements IProcessable, don't panic it's so cute.
 ```
 public interface IProcessable
    {
        Task<bool> Execute();
    }
  ```
  This is the result of a very simple implementation
```
  public class ConsoleWriteProcessable : IProcessable
    {
        private string _text;
        public ConsoleWriteProcessable(string text = "")
        {
            _text = text;
        }
        public async Task<bool> Execute()
        {
            Console.WriteLine(DateTime.Now + _text);
            await Task.Delay(1000);
            return true;
        }
    }
  ```