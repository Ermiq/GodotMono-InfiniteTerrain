using Godot;
using System;

public static class Statics
{
	public static void IterateSpiral(int sizeMinusOne, int startX, int startY, Action<int, int> callBack)
	{
		int x = 0; // current position; x
		int y = 0; // current position; y
		int d = 0; // current direction; 0=RIGHT, 1=DOWN, 2=LEFT, 3=UP
		int c = 0; // counter
		int s = 1; // chain size

		// starting point
		x = startX;
		y = startY;

		for (int k = 1; k <= sizeMinusOne; k++)
		{
			for (int j = 0; j < (k < sizeMinusOne ? 2 : 3); j++)
			{
				for (int i = 0; i < s; i++)
				{
					callBack(x, y);
					c++;

					switch (d)
					{
						case 0: y = y + 1; break;
						case 1: x = x + 1; break;
						case 2: y = y - 1; break;
						case 3: x = x - 1; break;
					}

					if (k + 1 > sizeMinusOne && j + 1 >= (k + 1 < sizeMinusOne ? 2 : 3) && i + 1 >= s)
						callBack(x, y);
				}
				d = (d + 1) % 4;
			}
			s = s + 1;
		}
	}

	public static void IterateLinear(int size, int startX, int startY, Action<int, int> callBack)
	{
		for (int y = -size + startY; y < size + startY + 1; y++)
			for (int x = -size + startX; x < size + startX + 1; x++)
				callBack(x, y);
	}
}
