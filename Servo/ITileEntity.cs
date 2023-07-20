namespace Servo;

public interface ITileEntity
{
    public void OnPlace(Map map, int x, int y);
    public void OnPreBreak(Map map);
    public void OnBreak(Map map);
}