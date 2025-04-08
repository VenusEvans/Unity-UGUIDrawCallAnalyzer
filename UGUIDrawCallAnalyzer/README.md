# UGUIDrawCallAnalyzer-Document
This is a tool designed to help quickly analyze draw calls under a specified Canvas.

![UGUIDrawCallAnalyzer](Image/UGUIDrawCallAnalyzer.jpeg)

To open the UGUI DrawCall Analyzer Window:

- Find the Canvas component in the Inspector panel and click **"Open UGUI DrawCall Analyzer Window"**, or
- Go to the menu bar: **Tools/Venus/UGUI DrawCall Analyzer**.

The window will display detailed draw call information for the current Canvas, such as which Graphics are merged into a single batch.

In the Scene view, Graphics belonging to the same batch will be highlighted with colored borders, making it easier to see how they are merged.

If you modify the UI, click the **"Analyze Canvas Draw Call"** button in the window to regenerate the draw call data.

- If the **Target Canvas** is null, the button text will change to **"Canvas is Null!"**.
- If there are no Graphics under the Canvas, the window will show **"No Graphic In Canvas!"**.
