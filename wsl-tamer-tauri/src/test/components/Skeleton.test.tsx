import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Skeleton, SkeletonCard, SkeletonList } from '../../components/Skeleton';

describe('Skeleton', () => {
  it('renders with default props', () => {
    const { container } = render(<Skeleton />);
    const el = container.firstChild as HTMLElement;

    expect(el).toHaveClass('skeleton', 'skeleton-text');
    expect(el.style.width).toBe('100%');
    expect(el.style.height).toBe('1rem');
  });

  it('applies circular variant class', () => {
    const { container } = render(<Skeleton variant="circular" />);
    expect(container.firstChild).toHaveClass('skeleton-circular');
  });

  it('applies rectangular variant class', () => {
    const { container } = render(<Skeleton variant="rectangular" />);
    expect(container.firstChild).toHaveClass('skeleton-rectangular');
  });

  it('accepts numeric width and height as pixels', () => {
    const { container } = render(<Skeleton width={200} height={40} />);
    const el = container.firstChild as HTMLElement;

    expect(el.style.width).toBe('200px');
    expect(el.style.height).toBe('40px');
  });

  it('accepts string width and height verbatim', () => {
    const { container } = render(<Skeleton width="50%" height="3rem" />);
    const el = container.firstChild as HTMLElement;

    expect(el.style.width).toBe('50%');
    expect(el.style.height).toBe('3rem');
  });

  it('applies custom className', () => {
    const { container } = render(<Skeleton className="my-custom" />);
    expect(container.firstChild).toHaveClass('my-custom');
  });
});

describe('SkeletonCard', () => {
  it('renders three skeleton lines inside a card', () => {
    const { container } = render(<SkeletonCard />);

    expect(container.querySelector('.skeleton-card')).toBeInTheDocument();
    const skeletons = container.querySelectorAll('.skeleton');
    expect(skeletons.length).toBe(3);
  });
});

describe('SkeletonList', () => {
  it('renders default 3 list items', () => {
    const { container } = render(<SkeletonList />);

    const items = container.querySelectorAll('.skeleton-list-item');
    expect(items.length).toBe(3);
  });

  it('renders custom count of list items', () => {
    const { container } = render(<SkeletonList count={5} />);

    const items = container.querySelectorAll('.skeleton-list-item');
    expect(items.length).toBe(5);
  });

  it('each list item has a circular avatar and two text lines', () => {
    const { container } = render(<SkeletonList count={1} />);

    const item = container.querySelector('.skeleton-list-item')!;
    expect(item.querySelector('.skeleton-circular')).toBeInTheDocument();

    const content = item.querySelector('.skeleton-list-content')!;
    expect(content.querySelectorAll('.skeleton').length).toBe(2);
  });
});
