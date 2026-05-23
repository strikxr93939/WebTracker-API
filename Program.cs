var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
});
var app = builder.Build();

using (var db = new TrackerContext())
{
    db.Database.EnsureCreated();
}


app.MapGet("/", () => "Daily Tracker API");
app.MapGet("/status", () => "Сервер запущен!");

app.MapGet("/today", () =>
{
    string path = @"D:\CODE10\C#\TaskTracker\TaskTracker\bin\Debug\net10.0\tracker.txt";

    if(!File.Exists(path))
    return Results.NotFound("Записей пока нет.");

    string[] lines = File.ReadAllLines(path);
    string last = lines[lines.Length - 1];
    string[] parts = last.Split(",");

    return Results.Ok(new
    {
        Date = DateTime.Parse(parts[0]).ToString("dd MMMM yyyy"),
        HoursPlayed = int.Parse(parts[1]),
        DidSomethingUseful = parts[2] == "True" || parts[2] == "да",
        Feeling = parts[3]
    });
});

app.MapGet( "/history", () =>
{
    string path = @"D:\CODE10\C#\TaskTracker\TaskTracker\bin\Debug\net10.0\tracker.txt";
    if(!File.Exists(path))
    return Results.NotFound("Записей пока нет.");

    string[] lines = File.ReadAllLines(path);
    int start = Math.Max(0, lines.Length - 7);

    var output = new System.Text.StringBuilder();

        for (int i = start; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            string date = DateTime.Parse(parts[0]).ToString("dd MMMM yyyy");
            string hours = parts[1];
            bool useful = parts[2] == "True" || parts[2] == "да";
            string feeling = parts[3];

            output.AppendLine(date);
            output.AppendLine($"Играл: {hours} ч");
            output.AppendLine($"Полезное: {(useful ? "да" : "нет")}");
            output.AppendLine($"Самочувствие: {feeling}");
            output.AppendLine("---");
        }

        return Results.Text(output.ToString());
});


app.MapPost("/add", (DayRecord record) =>
{
    using var db = new TrackerContext();
    record.Date = DateTime.Today.ToString("yyyy-MM-dd");
    db.Records.Add(record);
    db.SaveChanges();
    return Results.Ok("Запись сохранена!");
});

app.MapGet("/db/history", () =>
{
    using var db = new TrackerContext();
    var records = db.Records.ToList();

    var output = new System.Text.StringBuilder();

    foreach (var record in records)
    {
        string date = DateTime.Parse(record.Date).ToString("dd MMMM yyyy");
        output.AppendLine(date);
        output.AppendLine($"Играл: {record.HoursPlayed} ч");
        output.AppendLine($"Полезное: {(record.DidSomethingUseful ? "да" : "нет")}");
        output.AppendLine($"Самочувствие: {record.Feeling}");
        output.AppendLine("---");
    }

    return Results.Text(output.ToString());
});

app.MapGet("/db/today", () =>
{
   using var db = new TrackerContext();
   var last = db.Records.OrderByDescending(r => r.Id).FirstOrDefault();

   if (last == null)
    return Results.NotFound("Записей пока нет");

    var output = new System.Text.StringBuilder();
    output.AppendLine(DateTime.Parse(last.Date).ToString("dd MMMM yyyy"));
    output.AppendLine($"Играл :{last.HoursPlayed} ч");
    output.AppendLine($"Полезное :{(last.DidSomethingUseful ? "да" : "нет")} ");
    output.AppendLine($"Самочувствие :{last.Feeling}");

    return Results.Text(output.ToString());
});

app.MapGet("/dashboard", () =>
{
   using var db = new TrackerContext();
   var records = db.Records.OrderByDescending(r => r.Id).Take(7).ToList(); 
   
   var rows = new System.Text.StringBuilder();
   foreach (var record in records)
    {
        string date = DateTime.Parse(record.Date).ToString("dd MMMM yyyy");
        string useful = record.DidSomethingUseful ? "✅" : "❌";
        rows.Append($"<tr><td>{date}</td><td>{record.HoursPlayed} ч</td><td>{useful}</td><td>{record.Feeling}</td></tr>");
    }

    string css = "body{font-family:sans-serif;max-width:700px;margin:40px auto;background:#111;color:#eee;} h1{color:#7eb8f7;} table{width:100%;border-collapse:collapse;} th{background:#222;padding:10px;text-align:left;} td{padding:10px;border-bottom:1px solid #333;} input,select{margin:5px 0 10px;padding:8px;width:100%;background:#222;color:#eee;border:1px solid #444;} button{padding:10px 20px;background:#7eb8f7;color:#111;border:none;cursor:pointer;}";

    string html = $"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8">
        <title>DailyTracker</title>
        <style>{css}</style>
    </head>
    <body>
        <h1>📊 DailyTracker</h1>
        <form action="/add-form" method="post" style="margin-bottom:30px;">
            <label>Часов играл:</label><br>
            <input type="number" name="hoursPlayed" min="0" max="24"><br>
            <label>Сделал что-то полезное?</label><br>
            <select name="didSomethingUseful">
                <option value="true">Да</option>
                <option value="false">Нет</option>
            </select><br>
            <label>Как себя чувствуешь?</label><br>
            <input type="text" name="feeling"><br>
            <button type="submit">Сохранить</button>
        </form>
        <table>
            <tr><th>Дата</th><th>Играл</th><th>Полезное</th><th>Самочувствие</th></tr>
            {rows}
        </table>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html");
});

app.MapPost("/add-form", async (HttpContext context) =>
{
   var form = await context.Request.ReadFormAsync(); 
   var record = new DayRecord
   {
     Date = DateTime.Today.ToString("yyyy-MM-dd"),
     HoursPlayed = int.Parse(form["hoursPlayed"]),
     DidSomethingUseful = form["didSomethingUseful"] == "true",
     Feeling = form["feeling"]  
   };

   using var db = new TrackerContext();
   db.Records.Add(record);
   db.SaveChanges();

   context.Response.Redirect("/dashboard");
});

app.Run();
