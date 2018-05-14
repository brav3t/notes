// 1. feladat Egyszerű modulok: 2 LED, 2 gomb |jobb gomb -> jobb LED kék| bal gomb -> bal LED kék | mindkét gomb -> mindkét LED piros
void ProgramStarted()
{
	Debug.Print("Program Started");
	leftButton.ButtonPressed += buttonPressed;
	rightButton.ButtonPressed += buttonPressed;
	leftButton.ButtonReleased += buttonReleased;
	rightButton.buttonReleased += buttonReleased;
}

void buttonPressed(Button sender, Button.ButtonState state)			// gombnyomas esemenykezeloje
{
	if(leftButton.Pressed && rightButton.Pressed)
	{
		leftLED.TurnRed();
		rightED.TurnRed();
	}
	else
	{
		if(leftButton.Pressed)
		{
			leftLED.TurnBlue();
		}
		if(rightButton.Pressed)
		{
			rightED.TurnBlue();
		}
	}
}

void buttonReleased(Button sender, Button.ButtonState state)		// gomb felengedes esemenykezeloje
{
	if(sender == leftButton)
	{
		leftLED.TurnOff();
	}
	if(sender == rightButton)
	{
		rightED.TurnOff();
	}
}

// Idozites - Gadgeteer.Timer


public partial class Program
{
	private GT.Timer timer;

	void ProgramStarted()
	{
		Debug.Print("Program Started");
		timer = new GT.Timer(3000);
		timer.Tick += new GT.Timer.TickEventHandler(timer_Tick);
		timer.Start();
	}

	void timer_Tick(GT.Timer timer)
	{
		led.BlinkOnce(GT.Color.Red);
	}
}


//------------------------------------------------------------------------------------------------------------------

// 2. feladat Grafika alapjai | TE35 kijelzo használata 320x240 pixel | R, G, B, T aljzathoz kell csatlakoztatni

// Rajzolas SimpleGraphics interfesszel

void ProgramStarted()
{
	Debug.Print("Program Started");
	DrawSimpleGraphics();
}

private void DrawSimpleGraphics()
{
	display.SimpleGraphics.BackgroundColor.GT.Color.White;
	display.SimpleGraphics.DisplayEllipse(GT.Color.Cyan, 100, 100, 50, 50);
	display.SimpleGraphics.DisplayRectangle(GT.Color.Red, 200, 100, 80, 50);
	display.SimpleGraphics.DisplayText("ABC", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.FromRGB(100,77,98), 40, 150);
}

// Grafikus stopper alkalmazas -- amit oran is csinaltunk

public enum StopperStatus
{
	Started,
	Stopped
}

class Stopper
{
	public static readonly int TimerResolution = 100;
	public static readonly int BufferCapacity = 3;
	private Timer timer;
	public TimeSpan[] LastTimes { get; private set; }
	private int timeIndex;
	public TimeSpan MeasuredTime { get; private set; }
	public StopperStatus stopperStatus { get; private set; }
	public event EventHandler StopperUpdated;

	public Stopper()
	{
		LastTimes = new TimeSpan[BufferCapacity];									// rogzitett meresi eredmenyek, 3 db
		timer = new Timer(TimerResolution, Timer.BehaviorType.RunContinuously);		// milyen idokozonkent keruljon meghivasra ,ismetlodo mukodes
		timer.Tick += new Timer.TickEventHandler(timer_Tick);						
		stopperStatus =  StopperStatus.Stopped;
	}

	void timer_Tick(Timer timer)
	{
		MeasuredTime = MeasuredTime.Add(new TimeSpan(0, 0, 0, 0, TimerResolution));
		OnStopperUpdated();
	}

	private void OnStopperUpdated()
	{
		if(StopperUpdated != null)
		{
			StopperUpdated(this, EventArgs.Empty);
		}
	}

	public void Start()
	{
		MeasuredTime = new TimeSpan();
		timer.Start();
		stopperStatus = StopperStatus.Started;
	}

	public void Stop()
	{
		timer.Stop();
		stopperStatus = StopperStatus.Stopped;
	}

	public void NextState()
	{
		switch(stopperStatus)
		{
			case StopperStatus.Started:
				SaveMeasuredTime();
				Stop();
				break;
			case StopperStatus.Stopped:
				Start();
				break;
			default:
				break;
		}
	}

	private void SaveMeasuredTime()
	{
		LastTimes[timeIndex++ % BufferCapacity] = MeasuredTime;
	}

	public string Format(TimeSpan timeSpan)
	{
		string formattedString = String.Empty;
		formattedString = WithLeadingZero(timeSpan.Minutes) + ":" + WithLeadingZero(timeSpan.Seconds) + "." + timeSpan.Milliseconds / 100;
		return formattedString;
	}

	private string WithLeadingZero(int number)
	{
		if (number < 10)
		{
			return 0 + number.ToString();
		}
		else
		{
			return number.ToString();
		}
	}

	public string GetTimeText()
	{
		return Format(MeasuredTime);
	}
}

class Program
{
	public static readonly GT.Color MeasuredTimeColor = GT.Color.Red;
	public static readonly GT.Color LastTimesColor = GT.Color.White;
	private Stopper stopper;
	private Bitmap bitmap;

	void ProgramStarted()
	{
		stopper = new Stopper();
		stopper.StopperUpdated += new EventHandler(stopper_StopperUpdated);
		button.ButtonPressed += new Button.ButtonEventHandler(button_ButtonPressed);
		bitmap = new Bitmap((int)displayTE35.Width, (int)displayTE35.Height);
		UpdateUI();
	}

	void button_ButtonPressed(Button sender, Button.ButtonState state)
	{
		stopperStatus.NextState();
		UpdateUI();
	}

	void stopper_StopperUpdated(object sender, EventArgs e)
	{
		UpdateUI();
	}

	private void UpdateUI()
	{
		string timeText = stopper.GetTimeText();
		Font font = Resources.GetFont(Resources.FontResources.NinaB);
		bitmap.Clear();
		bitmap.DrawText(timeText, font, MeasuredTimeColor, 0, 0);
		TimeSpan[] lastTimes = stopper.LastTimes;
		for(int i = 0, i < lastTimes.Length, i++)
		{
			bitmap.DrawText(stopper.Format(lastTimes[i]), font, LastTimesColor, 0, ((i + 2) * font.Height));
		}
		displayTE35.SimpleGraphics.DisplayImage(bitmap, 0, 0);
	}
}

//------------------------------------------------------------------------------------------------------------------

// 3. feladat JoyStick | folyamatosan megjeleniti a joystick allapotat

private GT.Timer timer;
private Bitmap backbuffer;
private readonly int MarkerLength = 75;
private readonly int MarkerWidth = 3;
private int centerX;
private int centerY;
private readonly double JoystickThresold = 0.05;

private void Initialize()
{
	backbuffer = new Bitmap(displayTE35.Width, displayTE35.Height);
	timer = new GT.Timer(100);
	timer.Tick += timer_Tick;
	centerX = displayTE35.Width / 2;
	centerY = displayTE35.Height / 2;
}

void ProgramStarted()
{
	Debug.Print("Program Started");
	Initialize();
	timer.Start();
}

void timer_Tick(GT.Timer timer)
{
	Joystick.Position position = joystick.GetPosition();
	bool isPressed = joystick.IsPressed();
	backbuffer.Clear();
	PrintParameters(position.X, position.Y, isPressed);
	DrawDirection(position.X, position.Y);
	displayTE35.SimpleGraphics.DisplayImage(backbuffer, 0, 0);
}

private void PrintParameters(double x, double y, bool pressed)
{
	Font font = Resources.GetFont(Resources.FontResources.NinaB);
	GT.Color color = GT.Color.White;
	backbuffer.DrawText("X:" + x, font, color, 0, 0);
	backbuffer.DrawText("Y:" + y, font, color, 0, 25);

	if(pressed)
	{
		backbuffer.DrawText("Joystick is pressed", font, color, 0, 50);
	}
	else
	{
		backbuffer.DrawText("Joystick is released", font, color, 0, 50);	
	}
}

private void DrawDirection(double x, double y)
{
	if(System.Maths.Abs(x) >= JoystickThresold || System.Maths.Abs(y) >= JoystickThresold)
	{
		double angle = System.Maths.Atan2(x,y);
		int horizontalComponent = (int)(System.Maths.Cos(angle) * MarkerLength);
		int verticalComponent = (int)(System.Maths.Sin(angle) * MarkerLength);
		backbuffer.DrawLine(GT.Color.Red, MarkerWidth, centerX, centerY, centerX + horizontalComponent, centerY + verticalComponent);
	}
	else
	{
		backbuffer.DrawLine(GT.Color.Red, MarkerWidth, centerX, centerY, centerX, centerY);
	}
}



