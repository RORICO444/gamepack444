public class Constants
{
    public static class StoryPaths
    {
        public static string GetStoryPathByScene(string sceneName)
        {
            return sceneName switch
            {
                "Day1" => "assets/Resources/story/1.xlsx",
                "Day2" => "assets/Resources/story/2.xlsx",
                "Day3" => "assets/Resources/story/3.xlsx",
                "Hall" => "assets/Resources/story/4.xlsx",
                "Last" => "assets/Resources/story/5.xlsx",
                _ => "assets/Resources/story/default.xlsx"
            };
        }
    }
    
    // 保持兼容性
    public static string STORY_PATH = "assets/Resources/story/default.xlsx";
}