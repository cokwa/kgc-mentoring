#pragma once

#include <queue>

struct Task
{
	int type;
	int arg1, arg2;
};

extern std::queue<Task> tasks;

class Beverage
{
public:
	Beverage() : id(nextID++)
	{
		tasks.push({ 0, id });
	}

	Beverage(const Beverage& other) : Beverage()
	{
		*this = other;
	}

	Beverage& operator =(const Beverage& other)
	{
		tasks.push({ 1, other.id, id });
		tasks.push({ 2, other.id, id });
		tasks.push({ 3, other.id, id });
		return *this;
	}

private:
	int id;

	static int nextID;
};

void assignment1(Beverage tea, Beverage coffee);