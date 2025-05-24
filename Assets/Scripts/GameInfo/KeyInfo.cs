public class KeyInfo
{
    public float startT;
    public bool isJudged;
    public bool isSoundPlayedAtStart;
    public bool isSoundPlayedAtEnd;
    public KeyInfo(float startTime)
    {
        startT = startTime;
        isJudged = false;
        isSoundPlayedAtStart = false;
        isSoundPlayedAtEnd = false;
    }
}