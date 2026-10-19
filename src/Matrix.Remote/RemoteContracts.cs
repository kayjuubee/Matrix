namespace Matrix.Remote;

public sealed record RemotePhoto(string Id, string Coordinate, int Column, int Row, string PrintState);
public sealed record RemoteState(string? ProjectId, int Columns, int Rows, double TileAspect,
    int Total, int Remaining, IReadOnlyList<RemotePhoto> Photos, string? CurrentId,
    bool Slideshow, bool MosaicVisible, bool Monitoring, bool Printing, string? Printer, string? Error);
public sealed record RemoteCommand(string Action, string ProjectId, string RequestId, string? ImageId = null, bool ConfirmReprint = false);
public sealed record RemoteResult(bool Success, string Message);

public interface IRemoteController
{
    Task<RemoteState> ReadState();
    Task<byte[]?> ReadImage(string id, bool large = false);
    Task<RemoteResult> Execute(RemoteCommand command);
}
