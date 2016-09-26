Hello! Thank you for trying our Unity data visualization tool GraphIt!


GraphIt Instructions:

Installation:
Simply install the Unity package and ensure that GraphIt.cs and GraphItWindow.cs are in your Unity project. These files should exist under NothingIsEasy\Graphit in your Asset library. GraphIt only runs when your project is running in the Unity Editor, so it will have no impact on your final build.


How to use:
To create new graphs, from anywhere in your script, simply call
	GraphIt.Log("Graph Name", 5.0f ); 
5.0f is the value to add to this frame's log. Or if you simply want to increment your Graph's counter for this frame, call
	GraphIt.Log("Graph Name");

That's it! To view the graphs, select from the menu > Window > GraphIt. See below for the extended API.

Reading the graph:
Data to the right in he graph is newer, older to the left. The first bolded label is the graph name. The second label is average, both the average over the whole window, and the recent average (average of the last 120 frames). The third and fourth labels are the minimum value and the maximum value respectively.


Advanced graph setup:
There are 3 optional functions you can call to alter the setup of your Graph. Including the 0 Y-axis is useful for some graphs to more easily get a sense for the scale of changes. Without this option, graphs will be scaled between the minimum and max values. The default for this option is true. The sample window size option determines how many frames of data to track for each graph. The default window size is 2048.
	GraphIt.GraphSetup(string graph, bool include_0, int sample_window); //Allows you to specify both the inclusion of Y-axis 0, and how many samples to track.
	GraphIt.GraphSetupInclude0(string graph, bool include_0); 			 //Allows you to specify both the inclusion of Y-axis 0.
	GraphIt.GraphSetupSampleWindowSize(string graph, int sample_window); //Allows you to specify how many samples to track.

Manually stepping:
If you want to graph multiple frames worth of data in a single frame, you can log a value, and then step the graph to the next frame by calling 
	GraphIt.StepGraph("Graph Name");

Manually pausing/unpausing:
By default graphs are paused until you start logging data to them. Paused graphs won't step to the next frame. You may want to pause a graph if you know you won't be logging any useful information for a period of time. Such as during a loading screen or pause menu. Pausing a graph is also useful if you want to manually step frames, and then review your data. To pause and unpause, call the followin
    GraphIt.PauseGraph("Graph Name");
    GraphIt.UnpauseGraph("Graph Name");