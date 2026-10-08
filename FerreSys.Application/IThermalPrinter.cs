namespace FerreSys.Application;

public interface IThermalPrinter
{
    void Write(byte[] payload);
}
