#include "dont_touch.h"

#include <SFML/Graphics.hpp>

#include <vector>
#include <algorithm>

std::queue<Task> tasks;
int Beverage::nextID = 0;

Task PopTask()
{
	Task task = tasks.front();
	tasks.pop();

	return task;
}

class BeverageGraphics
{
private:
	sf::RectangleShape glass, content;

	float amount;
	sf::Color contentColor;

	sf::Vector2f position;

public:
	BeverageGraphics(int id) : position((id + 1) * 200.f, 400.f)
	{
		glass.setPosition(position);
		glass.setSize(sf::Vector2f(100, 200));
		glass.setOrigin(sf::Vector2f(50, 200));
		glass.setOutlineThickness(2.f);
		glass.setOutlineColor(sf::Color(127, 255, 255));
		glass.setFillColor(sf::Color::Transparent);
		
		content.setPosition(position);
		content.setSize(sf::Vector2f(100, 200));
		content.setOrigin(sf::Vector2f(50, 200));

		switch (id)
		{
		case 0:
			amount = 0.8f;
			contentColor = sf::Color::Green;
			break;

		case 1:
			amount = 0.8f;
			contentColor = sf::Color(127, 64, 64);
			break;

		default:
			amount = 0.f;
			contentColor = sf::Color::Transparent;
		}

		SetAmount(amount);
		SetContentColor(contentColor);
	}

	void ApproachTo(const BeverageGraphics& target, float t)
	{
		SetRotation(-80 * t);
		SetPosition(position * (1.f - t) + sf::Vector2f(target.position.x + 250, 150) * t);
	}

	void PourTo(BeverageGraphics& target, float t)
	{
		//SetAmount(amount * (1.f - t));
		target.SetAmount(target.amount * (1.f - t) + 0.8f * t);

		float mix = t;

		if (target.amount == 0.f)
		{
			mix = 1.f;
		}

		sf::Vector3f myContent(contentColor.r, contentColor.g, contentColor.b);
		sf::Vector3f targetsContent(target.contentColor.r, target.contentColor.g, target.contentColor.b);
		sf::Vector3f result = targetsContent * (1.f - mix) + myContent * mix;

		target.SetContentColor(sf::Color((sf::Uint8)result.x, (sf::Uint8)result.y, (sf::Uint8)result.z));

		if (t > 1.f)
		{
			target.amount = 0.8f;
			target.contentColor = target.content.getFillColor();
		}
	}

	void PutBack(const BeverageGraphics& pourTarget, float t)
	{
		SetRotation(-80 * (1.f - t));
		SetPosition(position * t + sf::Vector2f(pourTarget.position.x + 250, 150) * (1.f - t));
	}

	void SetPosition(const sf::Vector2f& position)
	{
		glass.setPosition(position);
		content.setPosition(position);
	}

	void SetRotation(float newRotation)
	{
		glass.setRotation(newRotation);
		content.setRotation(newRotation);
	}

	void SetAmount(float amount)
	{
		content.setScale(1.f, amount);
	}

	void SetContentColor(const sf::Color& color)
	{
		content.setFillColor(color);
	}

	void Draw(sf::RenderWindow& window) const
	{
		window.draw(glass);
		window.draw(content);
	}
};

int main()
{
	assignment1(Beverage(), Beverage());

	sf::RenderWindow window(sf::VideoMode(1000, 600), "SFML works!");

	sf::Clock clock;

	std::vector<BeverageGraphics> beverages;
	
	Task task{ -1 };
	bool proceed = true;

	while (window.isOpen())
	{
		if (proceed && !tasks.empty())
		{
			task = PopTask();
			if (task.type == 0)
			{
				beverages.push_back(BeverageGraphics(task.arg1));
			}
			else
			{
				proceed = false;
			}
		}

		sf::Event event;
		while (window.pollEvent(event))
		{
			if (event.type == sf::Event::Closed)
				window.close();
		}

		float t = clock.getElapsedTime().asSeconds();

		switch(task.type)
		{
		case 1:
			beverages[task.arg1].ApproachTo(beverages[task.arg2], t);
			break;

		case 2:
			beverages[task.arg1].PourTo(beverages[task.arg2], t);
			break;

		case 3:
			beverages[task.arg1].PutBack(beverages[task.arg2], t);
			break;
		}

		window.clear(sf::Color(127, 200, 255));

		for (const BeverageGraphics& beverageGraphics : beverages)
		{
			beverageGraphics.Draw(window);
		}

		window.display();

		if (t > 1.f)
		{
			clock.restart();

			if (tasks.empty())
			{
				task.type = -1;
			}
			else
			{
				proceed = true;
			}
		}
	}

	return 0;
}